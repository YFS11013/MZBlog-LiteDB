﻿using LiteDB;
using MZBlog.Core.Documents;
using System.Collections.Generic;
using System.Linq;

namespace MZBlog.Core.ViewProjections.Home
{
    public class TaggedBlogPostsViewModel
    {
        public IEnumerable<BlogPost> Posts { get; set; }

        public string Tag { get; set; }
    }

    public class TaggedBlogPostsBindingModel
    {
        public string Tag { get; set; }
    }

    public class TaggedBlogPostsViewProjection : IViewProjection<TaggedBlogPostsBindingModel, TaggedBlogPostsViewModel>
    {
        private readonly LiteDatabase _db;

        public TaggedBlogPostsViewProjection(LiteDatabase db)
        {
            _db = db;
        }

        public TaggedBlogPostsViewModel Project(TaggedBlogPostsBindingModel input)
        {
            var blogPostCol = _db.GetCollection<BlogPost>(DBTableNames.BlogPosts);
            var posts = (from p in blogPostCol.FindAll()
                         where p.IsPublished && p.Tags.Contains(input.Tag)
                         orderby p.PubDate descending
                         select p)
                     .ToList();
            if (posts.Count == 0)
                return null;
            var tagCol = _db.GetCollection<Tag>(DBTableNames.Tags);
            var tagName = tagCol.FindOne(x => x.Slug == input.Tag).Name;
            return new TaggedBlogPostsViewModel
            {
                Posts = posts,
                Tag = tagName
            };
        }
    }
}