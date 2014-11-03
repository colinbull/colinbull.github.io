@echo off
call tools\fsformatting.exe literate --processDirectory --lineNumbers false --inputDirectory  "code" --outputDirectory "_posts"

git add .
git commit -a -m %1
git push