@echo off

call .paket\paket restore
call "Fsi" generate.fsx

git add --all .
git commit -a -m %1
git push