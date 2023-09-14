# grabtex
Download images, including those in HTML meta-tag. Best use case is passing it URLs from a chat where you want to display a preview of the target destination.

# Usage
Add it to your manifest.json (replace vX.X.X with the version you want, for ex. v0.0.6):
```
{
  "dependencies": {
    ..,
    "com.xucian.upm.grabtex": "https://github.com/xucian/upm-grabtex#vX.X.X-upm"
  }
}
```

Add its dependencies too:
```
{
  "dependencies": {
    ..,
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.netpyoung.webp": "https://github.com/netpyoung/unity.webp.git?path=unity_project/Assets/unity.webp#0.3.9"
  }
}
```

Add this scoped registry (needed by a dependency of com.netpyoung.webp):
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
