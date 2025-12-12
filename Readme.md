Flow.Launcher.Plugin.PortViewer
==================
### Description
> View the Tcp or udp port process info and kill the process.



A plugin for the [Flow launcher](https://github.com/Flow-Launcher/Flow.Launcher).

### Usage

    pv <arguments>



#### all all port process
```shell
pv 13
```

![tcp listener port](Resources/defaults.png)


#### all tcp port process
```shell
pv tcp 5
```

![tcp listener port](Resources/tcp.png)

#### all udp port process
```shell
pv udp 53
```

![tcp listener port](Resources/udp.png)


#### Context Menu for kill process by Pid

![tcp listener port](Resources/context-kill.png)
