#!/bin/bash

mono .paket/paket.exe restore
fsharpi generate.fsx

git add --all .
git commit -a -m %1
git push