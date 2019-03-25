# Pinger
a small console tool to ping 4.2.2.4 (or custom defined host) for infinity but in intervals (in milliseconds). also shows an icon in systray which changes color depending on connection status.

the program also creates a new log file in the same path of exe, each time you run it.

###### Using command line
use below command line for initializing with custom hostname and interval:
```
Pinger.exe 8.8.8.8 3000
```
*above command will ping **8.8.8.8** continoiusly for each **3 seconds***

###### Normal use
on executing pinger.exe, console will ask for interval first (*default: 5 seconds*).
it waits 10 seconds for you to enter a value, otherwise goes for next input which is hostname/ip (*default: 4.2.2.4*).
likewise it waits more 10 seconds for you to enter a value for hostname.
