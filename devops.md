# DevOps notes

Collection of notes to manage my deployments.

## New Droplet

- Add SSH keys to authorized_keys
- Disallow password logins & change port, `systemctl restart sshd`
- apt update && apt upgrade && reboot
- add to NetData (get link from netdata website) (or update it)
- `TERM=rxvt` to ~/.bashrc, otherwise clear doesn't work within foot
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
