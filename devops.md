# DevOps notes

Collection of notes to manage my deployments.

## Maintenance

`journalctl --vacuum-time=7d` - clears logs from /var/log later than 7 days ago
`apt-get clean` - clears downloaded apt packages from /var/cache
`/var/cache/netdata` eats lots of space - monitor this

## Easy Swarm rebalance

(create aliases in local bashrc for these)

`docker service ls --format '{{.Name}}' | xargs -P 0 -I {} docker service update --force {}`

> This command rebalances all services across all nodes in parallel with zero downtime.

`docker stack ps NAME --filter "desired-state=running"`

> This command lists all services in a stack without failed/removed/old containers (current state).

## New Droplet

- Check there is no swap: free -h, swapon --show
- Check there is enough space: df -h
- Create swap: `fallocate -l 2G /swapfile` (2gb for 2gb system, 1gb for 1gb system)
- `chmod 600 /swapfile`
- `mkswap /swapfile`
- `swapon /swapfile`
- `echo "/swapfile none swap sw 0 0" | sudo tee -a /etc/fstab`
- `free -h`
- Check current swappiness: `cat /proc/sys/vm/swappiness` (should be close to 0 for most performance)
- `sysctl vm.swappiness=10` (for server 10 is good)
- Set up one more expensive thing to be less swappable
- `cat /proc/sys/vm/vfs_cache_pressure` (100?)
- `sysctl vm.vfs_cache_pressure=50` (set to at least 50)
- THESE BOTH are just for session, for permanent - edit /etc/sysctl.conf:
- `vm.swappiness=10`
- `vm.vfs_cache_pressure=50`
- reboot and test that changes persist

- Add SSH keys to authorized_keys
- Disallow password logins & change port, `systemctl restart sshd`
- apt update && apt upgrade && reboot
- add to NetData (get link from netdata website) (or update it)
- `TERM=rxvt` to ~/.bashrc, otherwise clear doesn't work within foot
- Alias ctop to bashrc: `alias ctop='docker run --rm -ti -v /var/run/docker.sock:/var/run/docker.sock quay.io/vektorlab/ctop:latest'`
- `apt install wireguard`
- `wg genkey | tee privatekey | wg pubkey > publickey`
- `cat privatekey && cat publickey`
- create `/etc/wireguard/wg0.conf`

```
[Interface]
PrivateKey = <client_private_key>
Address = 10.8.0.3/24

[Peer]
PublicKey = <server_public_key>
AllowedIPs = 10.8.0.0/24
Endpoint = typingrealm.com:WG_PORT
```

On the server, add another [Peer]:

`sudo wg set wg0 peer <new-client-public-key> allowed-ips 10.8.0.3/32`

Restart on server:
`systemctl restart wg-quick@wg0`

Check this record was added:
```
[Peer]
PublicKey = <client_public_key>
AllowedIPs = 10.8.0.3/32
```

`systemctl enable wg-quick@wg0`
`systemctl start wg-quick@wg0`

Allow forwarding between hosts (on server)

`iptables -A FORWARD -i wg0 -o wg0 -j ACCEPT`
`apt install iptables-persistent` (will persist them between boots, auto-configures on install)

### Swarm

Get token to add a worker node:
`docker swarm join-token worker` (use wg0 ip)

On worker node:

!! Install `docker-ce` following docker website guide.
Check version 28+

`(run token command)`

`docker service update --force <servicename>` - will rebalance between nodes, just for testing

### Folders

Create `/data` folder and respective folders for the needed projects inside, copy respective secrets there.
And copy DP (data protection) pfx too.

## Docker Swarm nodes

Add a label to my PC to set constraints later for dev env deployments:

`docker node update --label-add env=dev ivanpc`
`docker node update --label-add env=do-main do-main`
`docker node update --label-add env=do-worker do-worker`
