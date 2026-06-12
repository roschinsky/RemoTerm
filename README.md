# RemoteRM / RemoTerm

Lets say _RemoteRM_ could refer to something like "Remote Runtime Manager" or _RemoTerm_ is a kind of "Remote Termination".  

If you have one or more (primarily Windows-based) systems and want to run simple tasks repeatedly while you're away from your computer, _RemoteRM_ might be a good option for you.  

## How this works

_RemoteRM_ is a standalone binary that can be executed by Task Scheduler. It is up to you what's your execution interval - 5 minutes or once per hour. Set it up as it fits your needs.  

Once executed, _RemoteRM_ will proceed with these steps:

1. Try to get the configuration from API as `HTTP GET`
2. Run every action
3. Try to send back the execution log to the API as `HTTP POST`
4. Exit

## Config

There are two configurations:

### Remote Config

Is provided via API somewhere in your environment. The expected format looks like this:  

```json
{
  "onlyInLockedMode": false,
  "actions": [
    {
      "name": "end all calculations",
      "type": "Terminate",
      "payload": "calc"
    },
    {
      "name": "say hi",
      "type": "Message",
      "payload": "Caption...|Hey buddy!|Information"
    },
    {
      "name": "run terminal",
      "type": "Execute",
      "payload": "cmd.exe"
    }
  ]
}
```

### Local Config

Can be passed as command line options.

- `-h`, `--config-host`    = Host[:Port] of your config API
- `-i`, `--config-id`      = Configuration identifier; can be used to select different configurations
- `-l`, `--operate-locked` = Only executes actions in lock screen
- `-d`, `--debug`          = Application will not exit after processing; a tool window will bring up the execution log