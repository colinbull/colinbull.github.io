@echo off
REM call packages\FSharp.Formatting.CommandTool\tools\fsformatting.exe literate --processDirectory --lineNumbers false --inputDirectory  "code" --outputDirectory "_posts"

call "C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\fsi.exe" generate.fsx

git add .
git commit -a -m %1
git push