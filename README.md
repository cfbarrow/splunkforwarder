ft-security-sf-deploy
==========================

This module include script which will allows you to install Splunk agent on Windows Boxes UCS and AWS.

#### System requirements

For Windows users:

    Windows Server 2003, SP1
    Windows Server 2008
    Windows Server 2012
    Windows Vista
    Windows 7
    Windows 8
    Windows XP SP1

**Inputs & Props Configuration**

NOTE: if you'd like to add more information into splunk you can modify those two files:


Those files allow you to choose what you want to log, example:

```
#### FT Standards logs ####
[default]
host = ftwin-234-iwir-d
index = app_prod
_meta = systemCode::syscode

[monitor://C:\\logs\\]
whitelist = \\.log$
disabled = false
index=app_prod
ignoreOlderThan = 1d
```

You need to specify the file that you want  to log on the file props.conf:

```
monitor://C:\Logs\alertlogic.log]
```


#### Usage

    ftsecuritysf.exe


