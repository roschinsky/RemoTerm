# RemoteRM / RemoTerm

Lets say _RemoteRM_ could refer to something like "Remote Runtime Manager" or _RemoTerm_ is a kind of "Remote Termination".  

If you have one or more (primarily Windows-based) systems and want to run simple tasks repeatedly while you're away from your computer, _RemoteRM_ might be a good option for you.  

Just think of it as a minimalist, centralized way to perform a series of actions on one or more systems - either automatically or while someone is sitting right in front of them.  

I use it purely for convenience, so feel free to adopt it if you like.  

## How this works

_RemoteRM_ is a standalone binary that can be executed by Task Scheduler. It is up to you what's your execution interval - 5 minutes or once per hour.  
It makes no difference wether it will run on a single host

Set it up as it fits your needs.  

Once executed, _RemoteRM_ will proceed with these steps:

1. Try to get the configuration from API as `HTTP GET`
2. Run every action
3. Try to send back the execution log to the API as `HTTP POST`
4. Exit _(if not in debug mode)_

## Config

There are two configurations, you'll need to adjust:

### Remote Config

Is provided via API somewhere in your environment. The expected format looks like this:  

```json
{
  "id": "2",
  "onlyInLockedMode": true,
  "actions": [
    {
      "type": "Terminate",
      "name": "end all calculations",
      "payload": "calc"
    },
    {
      "type": "Message",
      "name": "say hi",
      "payload": "Caption...|Hey buddy!|Information"
    },
    {
      "type": "Execute",
      "name": "run terminal",
      "payload": "cmd.exe"
    }
  ]
}
```

### Local Config

Can be passed as command line options.

- `-h`, `--config-host`    = Host[:Port] of your config API
- `-i`, `--config-id`      = Configuration identifier; can be used to select different configurations
- `-l`, `--operate-locked` = Performs actions only if lock screen is active (can be overridden via API configuration)
- `-d`, `--debug`          = Application will not exit after processing; a tool window will bring up the execution log
