#!/usr/bin/env bash
# Pushes this job's Unity logs and results to the branch ci-logs-<name>, so
# they can be read with plain git (no artifact download needed).
#   push-logs.sh <name>
set -u
name="$1"
out="$RUNNER_TEMP/ci-logs-$name"
rm -rf "$out" && mkdir -p "$out"
sudo chown -R "$(id -u):$(id -g)" . 2>/dev/null || true

for p in artifacts Logs build/build-report.txt Library/Bee/tundra.log.json Library/Bee/fullprofile.json; do
  [ -e "$p" ] && cp -r "$p" "$out/" 2>/dev/null
done
# Large profiles are not needed; keep the logs small.
find "$out" -name fullprofile.json -size +5M -delete 2>/dev/null
{
  echo "run: $GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
  echo "commit: $GITHUB_SHA"
  echo "job: $name"
  date -u
} > "$out/README.txt"

cd "$out"
git init -q -b "ci-logs-$name"
git add -A
git -c user.name="github-actions" -c user.email="github-actions@users.noreply.github.com" \
  commit -q -m "Logs of $name for $GITHUB_SHA"
git push -q -f "https://x-access-token:$GITHUB_TOKEN@github.com/$GITHUB_REPOSITORY" "ci-logs-$name"
