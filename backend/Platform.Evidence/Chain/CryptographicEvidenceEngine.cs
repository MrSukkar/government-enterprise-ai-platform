using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Platform.Evidence.Chain;

public sealed class CryptographicEvidenceEngine(
    IEvidenceChainStore store,
    IEvidenceAccessAuthorizer accessAuthorizer,
    IEvidenceSigner signer,
    IEvidenceSignatureVerifier signatureVerifier)
{
    private static readonly EvidenceStage[] ApprovedSequence = Enum.GetValues<EvidenceStage>();

    public async Task<EvidenceEntry> AppendAsync(
        EvidenceAppendRequest request,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var authorization = await accessAuthorizer.AuthorizeAppendAsync(request, cancellationToken);
        ArgumentNullException.ThrowIfNull(authorization);
        authorization.Demand();

        var head = await store.LoadHeadAsync(request.ChainId, request.TenantId, cancellationToken);
        var sequence = head is null ? 0 : checked(head.Sequence + 1);
        if (sequence >= ApprovedSequence.Length || request.Stage != ApprovedSequence[sequence])
            throw new InvalidOperationException("Evidence stage is not the exact next approved stage.");
        if (head is not null)
            await ValidateHeadAsync(request, head, cancellationToken);

        var previousHash = head?.EntrySha256Digest ?? EvidenceValidation.GenesisSha256Digest;
        var unsigned = new EvidenceEntry(
            request.ChainId, request.TenantId, request.CorrelationId, sequence, request.Stage,
            request.ActorSubjectId, request.Classification, request.Purpose, request.PayloadSha256Digest,
            previousHash, EvidenceValidation.GenesisSha256Digest,
            new SignatureEnvelope("pending", "pending", "AA==", "pending", request.OccurredAt),
            authorization.EvidenceReference, request.TraceReferences, request.OccurredAt);
        var digest = ComputeDigest(unsigned);
        var signature = await signer.SignAsync(request.TenantId, digest, cancellationToken);
        ArgumentNullException.ThrowIfNull(signature);
        signature.Validate();
        if (signature.SignedAt < request.OccurredAt)
            throw new InvalidOperationException("Evidence was signed before it occurred.");
        var entry = unsigned with { EntrySha256Digest = digest, Signature = signature };
        entry.ValidateShape();

        var persisted = await store.AppendAtomicallyAsync(entry, sequence - 1, previousHash, cancellationToken);
        return ValidatePersisted(entry, persisted);
    }

    public async Task<EvidenceProofReport> VerifyAsync(
        EvidenceVerificationRequest request,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var authorization = await accessAuthorizer.AuthorizeVerificationAsync(request, cancellationToken);
        ArgumentNullException.ThrowIfNull(authorization);
        authorization.Demand();
        var entries = await store.LoadOrderedAsync(request.ChainId, request.TenantId,
            request.MaximumAuthorizedClassification, authorization.EvidenceReference, cancellationToken);
        if (entries.IsDefault) entries = [];

        var failures = ImmutableArray.CreateBuilder<string>();
        var proofs = ImmutableArray.CreateBuilder<EvidenceEntryProof>();
        var previousHash = EvidenceValidation.GenesisSha256Digest;
        string? correlationId = null;
        DateTimeOffset? previousTime = null;

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var classificationAuthorization = await accessAuthorizer.AuthorizeClassificationAsync(
                request, entry.Classification, cancellationToken);
            ArgumentNullException.ThrowIfNull(classificationAuthorization);
            classificationAuthorization.Demand();
            var shapeValid = true;
            try { entry.ValidateShape(); }
            catch (Exception) { shapeValid = false; failures.Add($"entry_{index}_shape_invalid"); }

            var identityValid = entry.ChainId == request.ChainId &&
                StringComparer.Ordinal.Equals(entry.TenantId, request.TenantId) && entry.Sequence == index;
            if (!identityValid) failures.Add($"entry_{index}_identity_invalid");
            if (index >= ApprovedSequence.Length || entry.Stage != ApprovedSequence[index])
                failures.Add($"entry_{index}_stage_order_invalid");
            correlationId ??= entry.CorrelationId;
            if (!StringComparer.Ordinal.Equals(correlationId, entry.CorrelationId))
                failures.Add($"entry_{index}_correlation_invalid");
            if (previousTime.HasValue && entry.OccurredAt < previousTime.Value)
                failures.Add($"entry_{index}_time_order_invalid");

            var linkValid = StringComparer.OrdinalIgnoreCase.Equals(entry.PreviousEntrySha256Digest, previousHash);
            if (!linkValid) failures.Add($"entry_{index}_chain_link_invalid");
            var computed = shapeValid ? ComputeDigest(entry) : string.Empty;
            var hashValid = shapeValid && StringComparer.OrdinalIgnoreCase.Equals(computed, entry.EntrySha256Digest);
            if (!hashValid) failures.Add($"entry_{index}_hash_invalid");
            var signatureValid = hashValid && entry.Signature is not null && await signatureVerifier.VerifyAsync(
                request.TenantId, entry.EntrySha256Digest, entry.Signature, cancellationToken);
            if (!signatureValid) failures.Add($"entry_{index}_signature_invalid");

            proofs.Add(new EvidenceEntryProof(entry.Sequence, entry.Stage, entry.EntrySha256Digest,
                entry.Signature?.Algorithm ?? string.Empty, entry.Signature?.KeyId ?? string.Empty, hashValid, signatureValid));
            previousHash = entry.EntrySha256Digest;
            previousTime = entry.OccurredAt;
        }

        if (entries.IsDefaultOrEmpty) failures.Add("chain_empty");
        if (entries.Length > ApprovedSequence.Length) failures.Add("chain_length_invalid");
        var complete = entries.Length == ApprovedSequence.Length &&
            entries[^1].Stage == EvidenceStage.Evidence;
        return new EvidenceProofReport(request.ChainId, request.TenantId, failures.Count == 0, complete,
            entries.IsDefaultOrEmpty ? null : entries[0].EntrySha256Digest,
            entries.IsDefaultOrEmpty ? null : entries[^1].EntrySha256Digest,
            proofs.ToImmutable(), failures.ToImmutable(), authorization.EvidenceReference, request.RequestedAt);
    }

    private async Task ValidateHeadAsync(
        EvidenceAppendRequest request,
        EvidenceEntry head,
        CancellationToken cancellationToken)
    {
        head.ValidateShape();
        if (head.ChainId != request.ChainId || !StringComparer.Ordinal.Equals(head.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(head.CorrelationId, request.CorrelationId) ||
            head.Sequence >= ApprovedSequence.Length || head.Stage != ApprovedSequence[head.Sequence] ||
            request.OccurredAt < head.OccurredAt ||
            !StringComparer.OrdinalIgnoreCase.Equals(ComputeDigest(head), head.EntrySha256Digest) ||
            !await signatureVerifier.VerifyAsync(request.TenantId, head.EntrySha256Digest, head.Signature, cancellationToken))
            throw new CryptographicException("Evidence chain head failed cryptographic validation.");
    }

    private static EvidenceEntry ValidatePersisted(EvidenceEntry expected, EvidenceEntry persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        persisted.ValidateShape();
        if (!Equivalent(expected, persisted))
            throw new InvalidOperationException("Evidence store changed the signed append-only entry.");
        return persisted;
    }

    private static bool Equivalent(EvidenceEntry left, EvidenceEntry right) =>
        left.ChainId == right.ChainId && left.Sequence == right.Sequence && left.Stage == right.Stage &&
        StringComparer.Ordinal.Equals(left.TenantId, right.TenantId) &&
        StringComparer.Ordinal.Equals(left.CorrelationId, right.CorrelationId) &&
        StringComparer.Ordinal.Equals(left.ActorSubjectId, right.ActorSubjectId) &&
        StringComparer.Ordinal.Equals(left.Classification, right.Classification) &&
        StringComparer.Ordinal.Equals(left.Purpose, right.Purpose) &&
        StringComparer.OrdinalIgnoreCase.Equals(left.PayloadSha256Digest, right.PayloadSha256Digest) &&
        StringComparer.OrdinalIgnoreCase.Equals(left.PreviousEntrySha256Digest, right.PreviousEntrySha256Digest) &&
        StringComparer.OrdinalIgnoreCase.Equals(left.EntrySha256Digest, right.EntrySha256Digest) &&
        left.Signature == right.Signature &&
        StringComparer.Ordinal.Equals(left.AuthorizationEvidenceReference, right.AuthorizationEvidenceReference) &&
        left.TraceReferences.SequenceEqual(right.TraceReferences, StringComparer.Ordinal) && left.OccurredAt == right.OccurredAt;

    internal static string ComputeDigest(EvidenceEntry entry)
    {
        var canonical = new StringBuilder();
        Add(canonical, entry.ChainId.ToString("D"));
        Add(canonical, entry.TenantId);
        Add(canonical, entry.CorrelationId);
        Add(canonical, entry.Sequence.ToString(CultureInfo.InvariantCulture));
        Add(canonical, entry.Stage.ToString());
        Add(canonical, entry.ActorSubjectId);
        Add(canonical, entry.Classification);
        Add(canonical, entry.Purpose);
        Add(canonical, entry.PayloadSha256Digest.ToLowerInvariant());
        Add(canonical, entry.PreviousEntrySha256Digest.ToLowerInvariant());
        Add(canonical, entry.AuthorizationEvidenceReference);
        Add(canonical, entry.OccurredAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        Add(canonical, entry.TraceReferences.Length.ToString(CultureInfo.InvariantCulture));
        foreach (var reference in entry.TraceReferences) Add(canonical, reference);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static void Add(StringBuilder builder, string value) =>
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
}
