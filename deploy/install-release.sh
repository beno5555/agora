#!/usr/bin/env bash
# Installs the release deploy.ps1 just uploaded to /tmp/agora.tar.gz and restarts the app.
# Runs as root on the droplet. The database lives in /var/lib/agora, so replacing /opt/agora/app keeps it.
set -euo pipefail

systemctl stop agora
rm -rf /opt/agora/app
mkdir -p /opt/agora/app
tar -xzf /tmp/agora.tar.gz -C /opt/agora/app
rm /tmp/agora.tar.gz
chmod +x /opt/agora/app/colosseumbackend
systemctl start agora

# Migrations run at startup, so give it a moment before declaring failure.
for _ in $(seq 1 30); do
    if curl -fsS http://localhost:8080/health > /dev/null; then
        echo "App is up (health check on localhost:8080 passed)"
        exit 0
    fi
    sleep 1
done

echo "App did not become healthy within 30 seconds. Last log lines:" >&2
journalctl -u agora -n 50 --no-pager >&2
exit 1
