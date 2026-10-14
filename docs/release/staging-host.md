# OVHcloud host preparation record

Verified 2026-10-14. The VPS is delivered and key-based SSH works. The selected
UK VPS-1 has Ubuntu 26.04, x86-64, 3814 MiB RAM and a 38 GiB root filesystem.
This record establishes host readiness, not PlantCare release acceptance.

## Configuration and evidence

- Installed available Ubuntu updates and the virtual-kernel update; rebooted
  and reconnected with the owner's key. Running kernel: `7.0.0-38-generic`.
- Installed Docker Engine `29.9.0` and Compose `v5.6.0` from Docker's official
  Ubuntu repository. The `hello-world` container passed. Docker starts at boot.
  Administrative Docker commands use `sudo`; the owner was not added to the
  root-equivalent Docker group.
- Enabled daily package-list refresh and unattended security upgrades, with
  Ubuntu's default security origins. The unattended-upgrades service is active.
  Automatic reboot was not enabled. Future kernel updates need maintenance.
- Enabled UFW, default deny incoming/allow outgoing. TCP 22 is allowed only
  from the current administrator IPv4; TCP 80/443 are allowed publicly for both
  address families. A timed firewall-disable rollback was scheduled first and
  cancelled only after a fresh SSH connection succeeded. UFW survived reboot.
- Added `/etc/ssh/sshd_config.d/00-plantcare.conf` with the values below. Checked
  `sshd -t` and effective `sshd -T` settings before reload; fresh key-based SSH
  succeeded after reload and reboot. Root/password/interactive SSH are disabled.

```text
PubkeyAuthentication yes
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin no
AuthenticationMethods publickey
```

- Created an owner-only `/home/ubuntu/plantcare-staging` directory. Uploaded the
  eight-file bundle for candidate `0acdb2db70a343d1aae977bf4bb19150deaaaf7a` under
  `releases/<candidate-sha>/`. All seven payload SHA-256 checks passed on Linux;
  both imported image IDs match the original local manifest after reboot.
- DNS `staging.plantacare.org` resolves to VPS IPv4 `57.129.182.78`. No AAAA
  record was configured. The apex remains reserved for the later release.
- After reboot/import: about 3361 MiB available RAM and 34 GiB free disk. These
  are idle measurements; SQL/API/worker/Caddy are not running yet. Only SSH
  listens among 22/80/443/1433/8080/8025. There is no public HTTPS app yet.

## Access and maintenance

The owner connects as `ubuntu` using their local private key through the Windows
SSH agent. The private key/passphrase stays on the owner's PC. Do not disable
key authentication or delete the owner's authorized key. Keep OVHcloud account
recovery/KVM access available.

SSH is restricted to the administrator's current public IPv4. Before changing
network/VPN, add the new trusted source while a working session remains open:

```bash
sudo ufw allow from <new-admin-ip> to any port 22 proto tcp
```

Verify a fresh key login from the new source, then remove only the obsolete
source rule. If already locked out, use OVHcloud's authenticated console/recovery
path to repair the firewall; do not delete the server or reinstall its OS.

Docker-published container ports can bypass UFW. Keep SQL/API ports unpublished;
only Caddy should publish web ports. UFW is not a provider-edge firewall. Provider
firewall setup and independent availability/backup monitoring remain open.

## Next prerequisite: transactional email

The owner requested a free provider. Brevo's free plan includes SMTP and up to
300 emails/day (checked 2026-10-14); account approval/sending limits still apply.
No Brevo account, SMTP key, domain authentication or delivery test is configured
by this record. The Gmail backup-alert connector is separate from app email.

1. Create a free Brevo account and add `plantacare.org` as the sending domain.
2. Use manual domain authentication; add exactly Brevo's generated TXT/CNAME
   records at Porkbun. Preserve unrelated DNS, avoid duplicate DMARC records,
   and do not change nameservers or invent DKIM/verification values.
3. Create the transactional sender, for example `notifications@plantacare.org`.
   Sending-domain authentication does not create an inbox for replies.
4. Obtain the SMTP login shown by Brevo and create a dedicated SMTP key (not
   an API key). Keep the key in private configuration, never chat/Git/screenshots.
   The SMTP endpoint is `smtp-relay.brevo.com`; select STARTTLS on port 587 for
   PlantCare's SMTP client. Confirm provider approval before delivery testing.
5. Configure a fresh server `.env`, then use [budget-staging.md](budget-staging.md)
   to run the explicit migration/runtime-user jobs and start the candidate.

Afterward verify trusted HTTPS, confirmation/reset email, full care journey,
worker/push, resource use under load, and independent VPS backups/recovery.
The Windows local-backup task does not protect this host. STAGE-01/OPS-01 stay open.

References: [Docker Ubuntu setup](https://docs.docker.com/engine/install/ubuntu/),
[Brevo free-plan details](https://help.brevo.com/hc/en-us/articles/208589409-About-Brevo-s-pricing-plans),
[Brevo SMTP setup](https://help.brevo.com/hc/en-us/articles/7924908994450-Send-transactional-emails-using-Brevo-SMTP).
