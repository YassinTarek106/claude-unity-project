---
name: extract-conversation
description: Extract the entire conversation into a clean Q&A markdown file
user_invocable: true
---

Extract the entire conversation into a clean Q&A markdown file.

Rules:
1. Review every message in the conversation from start to finish
2. Format each exchange as a Q&A pair using this exact format:

```
---

**Q:** <user's message, cleaned up for readability>

**A:** <your response, summarized concisely — include key actions taken, commands run, files created/modified, and important details. Keep it readable but thorough>
```

3. Save the file to `.claude/conversations/` with a descriptive kebab-case filename based on the conversation topic, e.g. `setup-mcp-server.md`
4. Add a title header at the top: `# Conversation: <Topic>`
5. Add a date line: `**Date:** YYYY-MM-DD`
6. Keep formatting clean and scannable — use code blocks for commands/paths, bullet points for lists
7. Do NOT truncate or skip any exchanges — capture the full conversation
8. If a response involved multiple steps, summarize them as a numbered list inside the answer
