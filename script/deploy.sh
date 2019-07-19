#!/bin/sh
echo "Deploy API docs to github pages.";
mkdir gh_pages;
cp -r Doc/html/ gh_pages/;
cd gh_pages && git init;
git config user.name "leancloud-bot";
git config user.email "ci@leancloud.cn";
git add .;
git commit -m "Deploy API docs to Github Pages [skip ci]";
git push -qf https://${TOKEN}@github.com/${TRAVIS_REPO_SLUG}.git master:gh-pages;
echo "done.";
cd ..