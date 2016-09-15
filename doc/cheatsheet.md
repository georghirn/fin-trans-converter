# Cheatsheet
## Some commands
```bash
git show-branch -a --list

gbp dch --new-version=0.0.2-1ubuntu1 --snapshot --auto debian/
gbp dch --new-version=0.0.2-1ubuntu1 --since= --snapshot debian/
gbp buildpackage --git-ignore-new -b

gbp dch --release --auto
git commit -m"Release ?.?.?-?" debian/changelog
gbp buildpackage --git-tag

pristine-tar commit
git push --all --tags

pristine-tar gendelta  fin-trans-converter_0.0.2.orig.tar.gz.delta
pristine-tar commit ../build-area/fin-trans-converter_0.0.2.orig.tar.gz
```

## Create new upstream version
Create changelog entry for new upstream version
"<major>.<minor>.<patch>-<debianversion><distribution><distroversion>~<testversion>",
e.g.: "0.0.2-1ubuntu1~test1" add an changelog entry "new upstream version" and commit
the changes.
```bash
gbp dch --new-version=0.0.2-1ubuntu1~test1 --auto --snapshot debian/
git commit -a -m "Created new upstream version 0.0.2-1ubuntu1~test1"
```

Checkout the upstream branch, merge it with the master branch and create a tag
for the new upstream version "upstream/v<major>.<minor>.<patch>",
e.g. "upstream/v0.0.2".
```bash
git checkout upstream
git merge master
git tag upstream/v0.0.2
```

Go back to the master branch and build the new upstream package.
```bash
git checkout master
gbp buildpackage --git-tag
```

Push all to the remote repository.
```bash
git push --all && git push --tags
```

```bash

```

## .git/gbp.conf
```bash
# --------------------------------------------------------
[DEFAULT]
# the default build command
builder = debuild -i -I -us -uc

# the default branch for upstream sources
upstream-branch = upstream

# the default branch for the debian patch
debian-branch = master

# whether to use colored output
color = auto

# the upstream tag format
upstream-tag = upstream/v%(version)s

# the debian tag format
debian-tag = debian/v%(version)s

# --------------------------------------------------------
[buildpackage]
pristine-tar = True
pristine-tar-commit = True
export-dir = ../build-area
tarball-dir = ../tarballs

# --------------------------------------------------------
[dch]
auto = True
meta = True
full = True
```
