@echo off
start tools\fsformatting.exe literate --processDirectory --inputDirectory  "code" --outputDirectory "_posts"

git add .
git commit -a -m "Updating blog posts"
git push