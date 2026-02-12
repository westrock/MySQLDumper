# MySQLDumper

## Overview

MySQLDumper runs the `mysqldump` command to create a logical backup of a MySQL database and performs maintenance on prior dump files (rotates/deletes old backups). It is a small console application intended to be run interactively, as a scheduled task, or from automation.

## Features

- Executes `mysqldump` and captures its output to a timestamped SQL file.
- Keeps backups in a configurable directory and deletes files older than a configurable number of days.
- Uses .NET Framework configuration (`appSettings`) for all settings — no code changes required to change behavior.

## Prerequisites

- Windows with .NET Framework 4.8 installed.
- `mysqldump.exe` available on the machine (from MySQL Server installation) and accessible to the account that runs the app.
- Appropriate filesystem permissions for the backup directory and the configuration file containing secrets.

## Configuration

Place non-sensitive settings in the project's `AppSettings.config`. Move sensitive command-line arguments (including the database password) into a separate file and reference it with the `file` attribute on the `appSettings` element. The application reads `ConfigurationManager.AppSettings["CommandArguments"]` without any code changes.

Example `AppSettings.config` (note `file="CommandArguments.config"`):

    <appSettings file="CommandArguments.config">
      <add key="CommandFileName" value="C:\Program Files\MySQL\MySQL Server 9.2\bin\mysqldump.exe" />
      <add key="OutputFile" value="D:\SqlBackups\log4om2_{DateTime}.sql" />
      <add key="BackupFileMask" value="log4om2*.sql" />
      <add key="BackupDirectory" value="D:\SqlBackups" />
      <add key="BackupAgeDays" value="14" />
    </appSettings>

Create the external `CommandArguments.config` (do NOT commit this file to git):

    <?xml version="1.0" encoding="utf-8"?>
    <appSettings>
      <add key="CommandArguments" value="-u root -pYourPasswordHere log4om2 --single-transaction --quick" />
    </appSettings>

Add the external file to `.gitignore` so credentials are never committed:

    # Ignore local command-arguments file (contains secrets)
    CommandArguments.config

## Secure the external file

Store `CommandArguments.config` outside the repository, or add it to `.gitignore`. Restrict access so only the account that runs the dumper can read it. Example PowerShell commands to create the file and grant read to the current user only:

    # secure-command-file.ps1
    New-Item -Path .\CommandArguments.config -ItemType File -Force
    icacls .\CommandArguments.config /inheritance:r
    icacls .\CommandArguments.config /grant:r "$env:USERNAME:R"

Adjust user/group and path as needed for service or scheduled-task accounts.

## Build and run

- Open the solution in Visual Studio 2022 via __Solution Explorer__.
- Build the project using __Build > Build Solution__.
- Run from Visual Studio or execute the compiled EXE found under `bin\Debug` or `bin\Release`.

Example command-line run:

    C:\path\to\MySQLDumper.exe

## Scheduling

To run automatically, create a Windows Scheduled Task that runs the EXE on the desired schedule. Set the Action to the full path of the EXE and the "Start in" directory to the EXE's folder. Ensure the task runs as the account that has read permission on `CommandArguments.config` and write permission to the backup directory.

## Security recommendations

- Prefer storing only the password in a credential store (Windows Credential Manager, DPAPI, or a secret manager) and reconstruct the args at runtime.
- Use a dedicated MySQL user with restricted privileges for backups.
- Keep `CommandArguments.config` outside the source tree or encrypted if stored centrally.

## Troubleshooting

- If `ConfigurationManager.AppSettings["CommandArguments"]` is empty, verify the external `CommandArguments.config` path is correct and readable by the process.
- Check Event Log entries created by the application (uses an event source named `MySQLDumper`) for diagnostic messages.

## License

Add your project license here.