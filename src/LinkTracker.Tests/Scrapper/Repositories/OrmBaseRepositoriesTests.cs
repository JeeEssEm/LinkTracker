using FluentResults;
using LinkTracker.Scrapper;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Configurations;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace LinkTracker.Tests.Scrapper.Repositories;

public class OrmBaseRepositoriesTests(ScrapperWebApplicationFactory factory) : BaseRepositoriesTests(factory, "ORM");
