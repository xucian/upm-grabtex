# grabtex
Download images, including those in HTML meta-tag. Best use case is passing it URLs from a chat where you want to display a preview of the target destination.

# Usage
Add it to your manifest.json (replace vX.X.X with the version you want, for ex. v0.0.6):

```
{
  "dependencies": {
	..,
	"com.xucian.upm.grabtex": "https://github.com/xucian/upm-grabtex#vX.X.X"
  }
}
```

You might also need to add this scoped registry (needed by a dependency of grabtex):
```
{
  "dependencies": {
	..
  },
  "scopedRegistries": [{
    "name": "Unity NuGet",
    "url": "https://unitynuget-registry.azurewebsites.net",
    "scopes": [
      "org.nuget"
    ]
  }]
}
```
