#!/bin/bash
# Hook: Prevent direct writes/edits to TaskManager/Migrations/ without manual review

# Read stdin (JSON input from Claude Code)
INPUT=$(cat)

# Extract file_path using grep/sed instead of jq
FILE_PATH=$(echo "$INPUT" | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 | sed 's/.*"file_path"[[:space:]]*:[[:space:]]*"//;s/"$//')

if echo "$FILE_PATH" | grep -q 'TaskManager/Migrations'; then
  echo '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "ask", "permissionDecisionReason": "This modifies a migration file in TaskManager/Migrations/. Please review the change manually before approving."}}'
  exit 0
fi

# Allow everything else
exit 0
