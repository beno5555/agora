#!/usr/bin/env bash
# One-time setup of a fresh Ubuntu 24.04 droplet for Agora. deploy.ps1 -Setup uploads this folder
# (with the generated agora.env) and runs it as root with the public host as the first argument and,
# optionally, a second host that redirects to it (the sslip.io address when the public host is another name).
# Safe to run again; it replaces agora.env, so a rerun gets a new JWT key and signs everyone out.
set -euo pipefail

host="$1"
redirect_from="${2:-}"
here="$(cd "$(dirname "$0")" && pwd)"
export DEBIAN_FRONTEND=noninteractive

# Swap: the droplet has none, and running out of memory would kill the app instead of slowing it down.
if [ ! -f /swapfile ]; then
    fallocate -l 1G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

# libicu74: globalization for the self-contained .NET app. ufw: the firewall. curl and gnupg: Caddy's apt repository.
apt-get update
apt-get install -y libicu74 ufw curl gnupg

# Caddy from its official apt repository (https://caddyserver.com/docs/install#debian-ubuntu-raspbian).
if [ ! -f /usr/share/keyrings/caddy-stable-archive-keyring.gpg ]; then
    curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | gpg --dearmor --yes -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
    curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' > /etc/apt/sources.list.d/caddy-stable.list
    chmod o+r /usr/share/keyrings/caddy-stable-archive-keyring.gpg /etc/apt/sources.list.d/caddy-stable.list
    apt-get update
fi
apt-get install -y caddy

# The app runs as its own user. /var/lib/agora (its home) holds the SQLite database; /opt/agora/app the release.
id agora &>/dev/null || useradd --system --home-dir /var/lib/agora --shell /usr/sbin/nologin agora
mkdir -p /opt/agora/app /var/lib/agora /etc/agora
chown agora:agora /var/lib/agora
chmod 700 /var/lib/agora

install -m 600 -o root -g root "$here/agora.env" /etc/agora/agora.env
install -m 644 "$here/agora.service" /etc/systemd/system/agora.service
sed "s/AGORA_HOST/$host/" "$here/Caddyfile" > /etc/caddy/Caddyfile
if [ -n "$redirect_from" ] && [ "$redirect_from" != "$host" ]; then
    # A temporary redirect (302), so browsers don't remember it and it can be removed later.
    printf '\n# The old address still works: it sends visitors to the public host.\n%s {\n\tredir https://%s{uri}\n}\n' \
        "$redirect_from" "$host" >> /etc/caddy/Caddyfile
fi
systemctl daemon-reload
systemctl enable agora
systemctl restart caddy

# Only SSH and the web ports are reachable; the app itself listens on localhost only.
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443
ufw --force enable

echo "Server set up for https://$host"
