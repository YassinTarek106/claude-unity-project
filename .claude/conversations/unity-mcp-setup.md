# Conversation: Unity MCP Server Setup & Claude Code Configuration

**Date:** 2026-04-09

---

**Q:** Install this MCP server: https://github.com/IvanMurzak/Unity-MCP

**A:** Fetched the README from the Unity-MCP GitHub repo and followed the installation steps:

1. **Added the Unity plugin via OpenUPM** — edited `Packages/manifest.json` to add:
   - A `scopedRegistries` entry for OpenUPM (`https://package.openupm.com`) scoped to `com.ivanmurzak`
   - The dependency `"com.ivanmurzak.unity.mcp": "1.0.0"`
2. **Created `.mcp.json`** at the project root with the MCP server config for Claude Code:
   ```json
   {
     "mcpServers": {
       "ai-game-developer": {
         "command": "D:/Work/Repos/claude-unity-project/Library/mcp-server/win-x64/unity-mcp-server.exe",
         "args": ["port=6100", "client-transport=stdio"]
       }
     }
   }
   ```
3. Noted that Node.js/npm were not available, so the CLI approach (`unity-mcp-cli`) was skipped.
4. Provided next steps: open Unity to download the plugin, use **Window > AI Game Developer** to configure, restart Claude Code to pick up the MCP config.

---

**Q:** I need you to create a `settings.json` in your `.claude` folder in this project to bypass permissions on Bash commands.

**A:** Created `.claude/settings.json` with the following content to auto-approve all Bash commands:

```json
{
  "permissions": {
    "allow": [
      "Bash(*)"
    ]
  }
}
```

---

**Q:** Add this conversation to an md file.

**A:** Created `.claude/conversations/unity-mcp-setup.md` summarizing the full conversation — including what was done (OpenUPM setup, `.mcp.json` creation, `settings.json` creation), next steps, and notes.

---

**Q:** Create a skill for extracting conversations. Whenever it is triggered, extract the entire conversation in Q&A format. Example: `Q: install this package` / `A: [agent's responses]`. Keep the format tidy and clean.

**A:** Created a custom Claude Code slash command at `.claude/commands/extract-conversation.md` with rules for:
- Reviewing the full conversation
- Formatting each exchange as `**Q:**` / `**A:**` pairs
- Saving to `.claude/conversations/` with a descriptive filename
- Including a title header and date

---

**Q:** Make this a Claude skill at `d:\Work\Repos\claude-unity-project\.claude\skills\extract-conversation.md`.

**A:** Moved the file to `.claude/skills/extract-conversation.md` and added proper skill frontmatter:

```yaml
---
name: extract-conversation
description: Extract the entire conversation into a clean Q&A markdown file
user_invocable: true
---
```

Removed the duplicate from `.claude/commands/`.

---

**Q:** `/extract-conversation`

**A:** Extracted the full conversation into this Q&A markdown file at `.claude/conversations/unity-mcp-setup.md`.