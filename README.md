# Twitter Video Uploader for Windows
[![Build status](https://ci.appveyor.com/api/projects/status/w1h37q3wt7dw08jt?svg=true)](https://ci.appveyor.com/project/hinaloe/twittervideouploader)

Upload movie to Twitter from Your Win PC!

![ScreenShot](http://puu.sh/iMHAN/c15dd16f97.png)

## Requirements

- .NET Framework 4.5.2
- CoreTweet 0.5.2+

## Tested on

Windows 7,8,10

## Can Upload

- File size MAX:512MB  
- Video formats: MP4

### Recommend

> - Duration should be between 0.5 seconds and ~~30 seconds (sync) /~~ **140 seconds (async)**
> - File size should not exceed ~~15 mb (sync) /~~ **512 mb (async)**
> - Dimensions should be between 32x32 and 1280x1024
> - Aspect ratio should be between 1:3 and 3:1
> - Frame rate should be 40fps or less
> - Must not have open GOP
> - Must use progressive scan
> - Must have 1:1 pixel aspect ratio
> - Only YUV 4:2:0 pixel format is supported.
> - Audio should be mono or stereo, not 5.1 or greater
> - Audio must be AAC with Low Complexity profile. High-Efficiency AAC is not supported.
>
> Quote from [Twitter Uploading Media](https://developer.twitter.com/en/docs/media/upload-media/uploading-media/media-best-practices)

## Build Environment
- **Require** Visual Studio 2017 or later (because using C# 7.x)
  - .NET Desktop development

## License

This software is licensed under the MIT License.
