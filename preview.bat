@echo off

call .paket\paket restore
call "fsi.exe" generate.fsx

