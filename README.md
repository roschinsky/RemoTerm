# RemoteRM / RemoTerm

Lets say _RemoteRM_ could refer to something like "Remote Runtime Manager" or _RemoTerm_ is a kind of "Remote Termination".  

If you have one or more (primarily Windows-based) systems and want to run simple tasks repeatedly while you're away from your computer, _RemoteRM_ might be a good option for you.  

Just think of it as a minimalist, centralized way to perform a series of actions on one or more systems - either automatically or while someone is sitting right in front of them.  

I use it purely for convenience, so feel free to adopt it if you like.  

## How this works

_RemoteRM_ is a standalone binary that can be executed by _Task Scheduler_ or direct execution. It is up to you what's your execution interval - 5 minutes or once per hour.  

Set it up as it fits your needs.  

Once executed, _RemoteRM_ will proceed with these steps:

1. Try to get the configuration from API as `HTTP GET`
2. Run every action, ...
    1. after given delay, if configured
    2. if condition matches, if configured (like "only in onlyInLockedMode && screen is locked out)
3. Try to send back the execution log to the API as `HTTP POST`
4. Exit _(if not in debug mode)_

## Config

There are two configurations, you'll need to adjust:

### Remote Client Config

Configurations are provided via API somewhere in your environment locally or in the cloud. Expected endpoint is `/api/configs/` - you can get the desired config via HTTP-GET by ID like `/api/configs/1`.  

The expected format looks like this:  

```json
{
  "id": "1",
  "update": false,
  "update-install": false,
  "public": true,
  "onlyInLockedMode": true,
  "delayActionsBy": "random120";
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
    }
  ]
}
```

### Local Client Config

Can be passed as command line options.

- `-h`, `--config-host`    = Host[:Port] of your config API
- `-c`, `--config-id`      = Configuration identifier; can be used to select different configurations
- `-l`, `--operate-locked` = Performs actions only if lock screen is active (can be overridden via API configuration)
- `-d`, `--debug`          = Application will not exit after processing; a tool window will bring up the execution log
- `-u`, `--update`         = Update RemoTerm in place from latest GitHub release
- `-i`, `--install`        = Install RemoTerm for automatic and unattended execution
- `-t`, `--token`          = Token for API access, if secured


## Remote Logging

Execution logs will be pushed to the endpoint `/api/logs/` via POST by ID like `/api/logs/1`.  