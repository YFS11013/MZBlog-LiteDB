﻿using LiteDB;
using MZBlog.Core;
using MZBlog.Core.Cache;
using MZBlog.Core.Extensions;
using MZBlog.Web.Features;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MZBlog.Web
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            StaticConfiguration.Caching.EnableRuntimeViewUpdates = true;
            StaticConfiguration.DisableErrorTraces = false;
            pipelines.OnError += ErrorHandler;
        }

        private Response ErrorHandler(NancyContext ctx, Exception ex)
        {
            if (ex is LiteException)
            {
                return "DB can't connect.";
            }
            return null;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register(typeof(ISpamShieldService), typeof(SpamShieldService));
            container.Register(typeof(ICache), typeof(RuntimeCache));

            RegisterIViewProjections(container);
            TagExtension.SetupViewProjectionFactory(container.Resolve<IViewProjectionFactory>());
            RegisterICommandInvoker(container);
            container.Register<Config>(this.Config);
            //container.Register(typeof(MongoDatabase), (cContainer, overloads) => Database);
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("scripts"));
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("content"));
        }

        private Config _config;

        public Config Config
        {
            get
            {
                if (_config == null)
                {
                    string dbDir = Path.Combine(this.RootPathProvider.GetRootPath(), "App_Data", "litedb");
                    if (!Directory.Exists(dbDir))
                    {
                        Directory.CreateDirectory(dbDir);
                    }

                    _config = new Config { DbPath = Path.Combine(dbDir, "blog.db") };
                }
                return _config;
            }
        }

        public static void RegisterICommandInvoker(TinyIoCContainer container)
        {
            var commandInvokerTypes = Assembly.GetAssembly(typeof(ICommandInvoker<,>))
                                              .DefinedTypes
                                              .Select(t => new
                                              {
                                                  Type = t.AsType(),
                                                  Interface = t.ImplementedInterfaces.FirstOrDefault(
                                                      i =>
                                                      i.IsGenericType() &&
                                                      i.GetGenericTypeDefinition() == typeof(ICommandInvoker<,>))
                                              })
                                              .Where(t => t.Interface != null)
                                              .ToArray();

            foreach (var commandInvokerType in commandInvokerTypes)
            {
                container.Register(commandInvokerType.Interface, commandInvokerType.Type);
            }

            container.Register(typeof(ICommandInvokerFactory), (cContainer, overloads) => new CommandInvokerFactory(cContainer));
        }

        public static void RegisterIViewProjections(TinyIoCContainer container)
        {
            var viewProjectionTypes = Assembly.GetAssembly(typeof(IViewProjection<,>))
                                              .DefinedTypes
                                              .Select(t => new
                                                               {
                                                                   Type = t.AsType(),
                                                                   Interface = t.ImplementedInterfaces.FirstOrDefault(
                                                                       i =>
                                                                       i.IsGenericType() &&
                                                                       i.GetGenericTypeDefinition() == typeof(IViewProjection<,>))
                                                               })
                                              .Where(t => t.Interface != null)
                                              .ToArray();

            foreach (var viewProjectionType in viewProjectionTypes)
            {
                container.Register(viewProjectionType.Interface, viewProjectionType.Type);
            }

            container.Register(typeof(IViewProjectionFactory), (cContainer, overloads) => new ViewProjectionFactory(cContainer));
        }
    }
}