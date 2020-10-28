# TcpingSharp

A simple, lightweight TCP Ping tool written in C#.

## Features

- Customizable timeout
- Ping multiple IPs of a single domain simutaneously
- Unix-like PING output
- Two pinging modes

## Usage

```
usage: tcping target [options]
  target: tcping target, can be a domain, hostname or IP
  options:
    -h, -?, --help          Print this message.
    -p, --port target_port  Set target port, default value is 80.
    -t, --timeout timeout   Set timeout, default value is 5000 (ms)
    -m, --multiple          Allow pinging multiple IPs simultaneously.
    -a, --animate           Animate output into a single line, incompatible
                              with -m option.
    -s, --stats             Periodically print statistics.
    -r, --rtt               Instead of showing time spent establishing a
                              TCP connection (~2x RTT), show half of the
                              value (~actual RTT).
```

## Implementation

To avoid flooding the server with TCP connection requests, TcpingSharp will wait for 1 second before trying again.

To establish a TCP connection, these requests are sent/received:

```
1 -> [SYN]
2 <- [SYN,ACK]
3 -> [ACK]
4 -> [FIN,ACK]
5 <- [FIN,ACK]
6 -> [ACK]
```

### Without `-r` option

TcpingSharp measures the time

- from **the first SYN packet is sent**
- to **the last ACK packet is sent**

Which is the time cost to establish a TCP connection and in theory, the sum of two client <-> server transmissions' and 5 packets' (1-2 and 3-5) time. Then TcpingSharp closes the connection safely, if established successfully.

### With the `-r` option

TcpingSharp still measures the same time as running without `-r` option, but the result time is always **divided in half**. So every result is the average time of two client <-> server transmissions in 5 packets.

### Accuracy

TcpingSharp is currently unable to give an accurate time of a single round-trip. Since even with the `-r` option, the average value is consisted of two different transmissions.

The first RTT is measured by:

```
-> [SYN]
<- [SYN,ACK]
```

The second RTT is measured by:

```
-> [ACK]
-> [FIN,ACK]
<- [FIN,ACK]
```
