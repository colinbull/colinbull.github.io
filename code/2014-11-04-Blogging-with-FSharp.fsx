(*** raw ***)
---
layout: page
title: Blogging with F#, GitHub and Jekyll
---

(**

##Introduction
    
Recently I decided I wanted to move my blog from Wordpress, to a far lighter-weight platform. The platform I chose to host my new blog on was [Github Pages](http://pages.github.com). Basically if you have a github account you get a free pages repository where you can host a blog. You simply create a repository with a name that matches the format `{username}.github.io` and that's basically it. You can now create a `index.htm` page place it into your repository and away you go.

Unfortunately having a completely static site isn't massively useful for a blog. At the very least, you are going to want a templating engine of some sort. Fortunately Github pages, comes armed with [jekyll](https://github.com/jekyll/jekyll) which is a blog-aware static site generator. Jekyll relies quite heavily on having the correct folder structure, this had me chasing my tail for a moment then I found the superb [poole](https://github.com/poole/poole) which generates all of the layout a creates a nice looking minimalist blog. Happy Days!
    
To add a post you simply create a `*.md' or '*.html' and save it to the posts directory, push your changes. So where does F# come in?

##Leveraging FSharp.Formatting

[FSharp.Formatting](https://github.com/tpetricek/FSharp.Formatting) isa tool
*)
