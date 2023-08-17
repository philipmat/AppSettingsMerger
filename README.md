# AppSettings Merger

The typical ASP.NET Core project supports stacking `appSetttings.json` files
so that more specific files, e.g. `appSettings.{Environment}.json` files, overwrite
the more generic `appSettings.json` file.

The purpose is to have most settings placed in `appSettings.json` and
place in the environment overwrite files only those settings which are different
from the base-line.

In my experience, long-running projects tend to duplicate settings between the
`appSettings.Development.json` and `appSettings.Production.json`
or `appSettings.Staging.json`, and these settings no longer get migrated back
into the `appSettings.json`.  
This, in turn, makes configuration management a bit more difficult over time,
as well as other operations like PR reviews, deployments, etc.

This project solves this case where settings are spread inefficiently over several
files and attempts to re-create a base `appSettings.json` file and only keep
those values that are different in other files.

## Operation

The parameters of the project are a list of files to inspect and attempt to merge.  
The "algorithm" is fairly naive so the `appSettings.json` file should be specified first.

For example, assuming:

* `appSettings.json` contains:

```json
{
  "Publish": {
    "TopicName": "books-read"
  }
}
```

* `appSettings.Development.json` contains:

```json
{
"Publish": {
    "TopicName": "books-read-dev",
    "MaxMessages": 5
},
"ConnectionStrings": {
    "Default": "Server=localhost;Database=books;Trusted_Connection=True"
}
}
```

* `appSettings.Production.json` contains:

```json
{
  "Publish": {
    "TopicName": "books-read",
    "MaxMessages": 10
  },
  "ConnectionStrings": {
    "Default": "Server=my-prod-serve;Database=books;Trusted_Connection=True"
  }
}
```

Calling AppSettingsMerger with
`appSettings.json appSettings.Development.json appSettings.Production.json`
will result in:

```json
// appSettings.json
{
  "Publish": {
    "TopicName": "books-read",
    "MaxMessages": 5
  },
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=books;Trusted_Connection=True"
  }
}

// appSettings.Development.json
{
  "Publish": {
    "TopicName": "books-read-dev"
  }
}

// appSettings.Production.json
{
  "Publish": {
    "MaxMessages": 10
  },
  "ConnectionStrings": {
    "Default": "Server=my-prod-serve;Database=books;Trusted_Connection=True"
  }
}
```

Each environment file now contains the values that differ from `appSettings.json`
making, in this author's humble opinion, easier to reason about settings for each
environment.

## How to run

There are no releases at this time. Clone the project then use `dotnet run --project`
to run it.

For example, if you cloned it in `~/Projects/AppSettingsMerger` and you would like
to merge the files in `~/Projects/MyCompany/MyWebApp/MyWebApp.API`,
you would navigate to `~/Projects/MyCompany/MyWebApp/MyWebApp.API` and then
invoke:  
`dotnet run --project=~/Projects/AppSettingsMerger/AppSettingsMerger.csproj appSettings.json appSettings.Development.json appSettings.Production.json`

There is a convenient `--dry-run` flag you can specify to show the output, without
altering the files, but typically I found that leaving it out, having AppSettingsMerger
change the files, then using even the barest VCS commands to diff the changes
produced better results.  
After all, undoing is just a `git checkout -f` away.