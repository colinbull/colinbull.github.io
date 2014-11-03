@echo off
call packages\FSharp.Formatting.CommandTool\tools\fsformatting.exe literate --processDirectory --templateFile "code/post_template.html" --lineNumbers false --inputDirectory  "code" --outputDirectory "_posts"

git add .
git commit -a -m %1
git push