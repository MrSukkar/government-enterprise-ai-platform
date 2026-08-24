# Codex Remote — Mobile Control Workflow

## Initial setup

1. Install or update the official ChatGPT app on the phone.
2. Sign in with the same OpenAI account used by Codex on the laptop.
3. Keep the Codex desktop app running on the Windows laptop and keep the laptop online and awake.
4. In the mobile app, open **Codex / Remote**.
5. Select the connected Windows host.
6. Select the `GovernmentEnterpriseAIPlatform` workspace and the `main` checkout for normal project work.
7. Open this project task or start a project task scoped to that workspace.

If Remote or the Windows host does not appear, update both apps, confirm the same account is active, and check that the desktop host is online. Availability can depend on the account and app rollout.

## Recommended controls

- Use **Queue** as the default for a new request that should wait until the current operation finishes.
- Use **Steer** only to correct work already in progress.
- Use a side chat for explanations that should not interrupt the main implementation task.
- Use a durable Goal for a multi-turn outcome; use Plan mode when a risky change needs boundaries before implementation.
- Approve only commands whose target and effect are clear.

## Project operating prompt

Use this concise instruction from mobile:

> Continue Government Enterprise AI Platform from the actual repository state. Follow AGENTS.md, the approved Master Specification, the fixed phase gate, and project-os/project-state.json. Verify before committing and report only decisions that require my approval.

## Security boundary

Remote is a control plane, not a Windows administrator bypass. Windows elevation, new credentials, destructive external actions, architectural Change Requests, and public deployment still require explicit approval.

