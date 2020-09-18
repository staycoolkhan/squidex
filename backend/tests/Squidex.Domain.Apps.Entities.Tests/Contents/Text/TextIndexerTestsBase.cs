﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Entities.Apps;
using Squidex.Domain.Apps.Entities.Contents.Text.State;
using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Domain.Apps.Events.Contents;
using Squidex.Infrastructure;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Infrastructure.Validation;
using Xunit;

#pragma warning disable SA1114 // Parameter list should follow declaration
#pragma warning disable SA1115 // Parameter should follow comma
#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor

namespace Squidex.Domain.Apps.Entities.Contents.Text
{
    public abstract class TextIndexerTestsBase
    {
        private readonly List<DomainId> ids1 = new List<DomainId> { DomainId.NewGuid() };
        private readonly List<DomainId> ids2 = new List<DomainId> { DomainId.NewGuid() };
        private readonly NamedId<DomainId> appId = NamedId.Of(DomainId.NewGuid(), "my-app");
        private readonly NamedId<DomainId> schemaId = NamedId.Of(DomainId.NewGuid(), "my-schema");
        private readonly IAppEntity app;

        private delegate Task IndexOperation(TextIndexingProcess process);

        public abstract IIndexerFactory Factory { get; }

        public virtual bool SupportsCleanup { get; set; } = false;

        public virtual bool SupportsSearchSyntax { get; set; } = true;

        public virtual bool SupportsMultiLanguage { get; set; } = true;

        public virtual InMemoryTextIndexerState State { get; } = new InMemoryTextIndexerState();

        protected TextIndexerTestsBase()
        {
            app =
                Mocks.App(NamedId.Of(DomainId.NewGuid(), "my-app"),
                    Language.DE,
                    Language.EN);
        }

        [SkippableFact]
        public async Task Should_throw_exception_for_invalid_query()
        {
            Skip.IfNot(SupportsSearchSyntax);

            await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await TestCombinations(Search(expected: null, text: "~hello"));
            });
        }

        [SkippableFact]
        public async Task Should_index_invariant_content_and_retrieve_with_fuzzy()
        {
            Skip.IfNot(SupportsSearchSyntax);

            await TestCombinations(
                Create(ids1[0], "iv", "Hello"),
                Create(ids2[0], "iv", "World"),

                Search(expected: ids1, text: "helo~"),
                Search(expected: ids2, text: "wold~", SearchScope.All)
            );
        }

        [SkippableFact]
        public async Task Should_search_by_field()
        {
            Skip.IfNot(SupportsSearchSyntax);

            await TestCombinations(
                Create(ids1[0], "en", "City"),
                Create(ids2[0], "de", "Stadt"),

                Search(expected: ids1, text: "en:city"),
                Search(expected: ids2, text: "de:Stadt")
            );
        }

        [Fact]
        public async Task Should_index_localized_content_and_retrieve()
        {
            if (SupportsMultiLanguage)
            {
                await TestCombinations(
                    Create(ids1[0], "de", "Stadt und Land and Fluss"),

                    Create(ids2[0], "en", "City and Country und River"),

                    Search(expected: ids1, text: "Stadt"),
                    Search(expected: ids2, text: "City"),

                    Search(expected: ids1, text: "and"),
                    Search(expected: ids2, text: "und")
                );
            }
            else
            {
                var both = ids2.Union(ids1).ToList();

                await TestCombinations(
                    Create(ids1[0], "de", "Stadt und Land and Fluss"),

                    Create(ids2[0], "en", "City and Country und River"),

                    Search(expected: ids1, text: "Stadt"),
                    Search(expected: ids2, text: "City"),

                    Search(expected: null, text: "and"),
                    Search(expected: both, text: "und")
                );
            }
        }

        [Fact]
        public async Task Should_index_invariant_content_and_retrieve()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "Hello"),
                Create(ids2[0], "iv", "World"),

                Search(expected: ids1, text: "Hello"),
                Search(expected: ids2, text: "World"),

                Search(expected: null, text: "Hello", SearchScope.Published),
                Search(expected: null, text: "World", SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_update_draft_only()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "V1"),

                Update(ids1[0], "iv", "V2"),

                Search(expected: null, text: "V1", target: SearchScope.All),
                Search(expected: null, text: "V1", target: SearchScope.Published),

                Search(expected: ids1, text: "V2", target: SearchScope.All),
                Search(expected: null, text: "V2", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_update_draft_only_multiple_times()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "V1"),

                Update(ids1[0], "iv", "V2"),
                Update(ids1[0], "iv", "V3"),

                Search(expected: null, text: "V2", target: SearchScope.All),
                Search(expected: null, text: "V2", target: SearchScope.Published),

                Search(expected: ids1, text: "V3", target: SearchScope.All),
                Search(expected: null, text: "V3", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_also_serve_published_after_publish()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "V1"),

                Publish(ids1[0]),

                Search(expected: ids1, text: "V1", target: SearchScope.All),
                Search(expected: ids1, text: "V1", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_also_update_published_content()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "V1"),

                Publish(ids1[0]),

                Update(ids1[0], "iv", "V2"),

                Search(expected: null, text: "V1", target: SearchScope.All),
                Search(expected: null, text: "V1", target: SearchScope.Published),

                Search(expected: ids1, text: "V2", target: SearchScope.All),
                Search(expected: ids1, text: "V2", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_also_update_published_content_multiple_times()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "V1"),

                Publish(ids1[0]),

                Update(ids1[0], "iv", "V2"),
                Update(ids1[0], "iv", "V3"),

                Search(expected: null, text: "V2", target: SearchScope.All),
                Search(expected: null, text: "V2", target: SearchScope.Published),

                Search(expected: ids1, text: "V3", target: SearchScope.All),
                Search(expected: ids1, text: "V3", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_simulate_new_version()
        {
            await TestCombinations(0,
                Create(ids1[0], "iv", "V1"),

                // Publish the content.
                Publish(ids1[0]),

                Search(expected: ids1, text: "V1", target: SearchScope.All),
                Search(expected: ids1, text: "V1", target: SearchScope.Published),

                // Create a new version, the value is still the same as old version.
                CreateDraft(ids1[0]),

                Search(expected: ids1, text: "V1", target: SearchScope.All),
                Search(expected: ids1, text: "V1", target: SearchScope.Published),

                // Make an update, this updates the new version only.
                Update(ids1[0], "iv", "V2"),

                Search(expected: null, text: "V1", target: SearchScope.All),
                Search(expected: ids1, text: "V1", target: SearchScope.Published),

                Search(expected: ids1, text: "V2", target: SearchScope.All),
                Search(expected: null, text: "V2", target: SearchScope.Published),

                // Publish the new version to get rid of the "V1" version.
                Publish(ids1[0]),

                Search(expected: null, text: "V1", target: SearchScope.All),
                Search(expected: null, text: "V1", target: SearchScope.Published),

                Search(expected: ids1, text: "V2", target: SearchScope.All),
                Search(expected: ids1, text: "V2", target: SearchScope.Published),

                // Unpublish the version
                Unpublish(ids1[0]),

                Search(expected: ids1, text: "V2", target: SearchScope.All),
                Search(expected: null, text: "V2", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_simulate_new_version_with_migration()
        {
            await TestCombinations(0,
                Create(ids1[0], "iv", "V1"),

                // Publish the content.
                Publish(ids1[0]),

                Search(expected: ids1, text: "V1", target: SearchScope.All),
                Search(expected: ids1, text: "V1", target: SearchScope.Published),

                // Create a new version, his updates the new version also.
                CreateDraftWithData(ids1[0], "iv", "V2"),

                Search(expected: null, text: "V1", target: SearchScope.All),
                Search(expected: ids1, text: "V1", target: SearchScope.Published),

                Search(expected: ids1, text: "V2", target: SearchScope.All),
                Search(expected: null, text: "V2", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_simulate_content_reversion()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "V1"),

                // Publish the content.
                Publish(ids1[0]),

                // Create a new version, the value is still the same as old version.
                CreateDraft(ids1[0]),

                // Make an update, this updates the new version only.
                Update(ids1[0], "iv", "V2"),

                // Make an update, this updates the new version only.
                DeleteDraft(ids1[0]),

                Search(expected: ids1, text: "V1", target: SearchScope.All),
                Search(expected: ids1, text: "V1", target: SearchScope.Published),

                Search(expected: null, text: "V2", target: SearchScope.All),
                Search(expected: null, text: "V2", target: SearchScope.Published),

                // Make an update, this updates the current version only.
                Update(ids1[0], "iv", "Night"),

                Search(expected: ids1, text: "Night", target: SearchScope.All),
                Search(expected: ids1, text: "Night", target: SearchScope.Published)
            );
        }

        [Fact]
        public async Task Should_delete_documents_from_index()
        {
            await TestCombinations(
                Create(ids1[0], "iv", "Hello"),
                Create(ids2[0], "iv", "World"),

                Search(expected: ids1, text: "Hello"),
                Search(expected: ids2, text: "World"),

                Delete(ids1[0]),

                Search(expected: null, text: "Hello"),
                Search(expected: ids2, text: "World")
            );
        }

        private IndexOperation Create(DomainId id, string language, string text)
        {
            var data =
                new NamedContentData()
                    .AddField("text",
                        new ContentFieldData()
                            .AddValue(language, text));

            return Op(id, new ContentCreated { Data = data });
        }

        private IndexOperation Update(DomainId id, string language, string text)
        {
            var data =
                new NamedContentData()
                    .AddField("text",
                        new ContentFieldData()
                            .AddValue(language, text));

            return Op(id, new ContentUpdated { Data = data });
        }

        private IndexOperation CreateDraftWithData(DomainId id, string language, string text)
        {
            var data =
                new NamedContentData()
                    .AddField("text",
                        new ContentFieldData()
                            .AddValue(language, text));

            return Op(id, new ContentDraftCreated { MigratedData = data });
        }

        private IndexOperation CreateDraft(DomainId id)
        {
            return Op(id, new ContentDraftCreated());
        }

        private IndexOperation Publish(DomainId id)
        {
            return Op(id, new ContentStatusChanged { Status = Status.Published });
        }

        private IndexOperation Unpublish( DomainId id)
        {
            return Op(id, new ContentStatusChanged { Status = Status.Draft });
        }

        private IndexOperation DeleteDraft(DomainId id)
        {
            return Op(id, new ContentDraftDeleted());
        }

        private IndexOperation Delete(DomainId id)
        {
            return Op(id, new ContentDeleted());
        }

        private IndexOperation Op(DomainId id, ContentEvent contentEvent)
        {
            contentEvent.ContentId = id;
            contentEvent.AppId = appId;
            contentEvent.SchemaId = schemaId;

            return p => p.On(Enumerable.Repeat(Envelope.Create<IEvent>(contentEvent), 1));
        }

        private IndexOperation Search(List<DomainId>? expected, string text, SearchScope target = SearchScope.All)
        {
            return async p =>
            {
                var searchFilter = SearchFilter.ShouldHaveSchemas(schemaId.Id);

                var result = await p.TextIndex.SearchAsync(text, app, searchFilter, target);

                if (expected != null)
                {
                    Assert.Equal(expected, result);
                }
                else
                {
                    Assert.Empty(result);
                }
            };
        }

        private async Task TestCombinations(params IndexOperation[] actions)
        {
            if (SupportsCleanup)
            {
                for (var i = 0; i < actions.Length; i++)
                {
                    await TestCombinations(i, actions);
                }
            }
            else
            {
                await TestCombinations(0, actions);
            }
        }

        private async Task TestCombinations(int firstSteps, params IndexOperation[] actions)
        {
            await ExecuteAsync(async sut =>
            {
                foreach (var action in actions.Take(firstSteps))
                {
                    await action(sut);
                }
            });

            await ExecuteAsync(async sut =>
            {
                foreach (var action in actions.Skip(firstSteps))
                {
                    await action(sut);
                }
            });
        }

        private async Task ExecuteAsync(IndexOperation action)
        {
            var indexer = await Factory.CreateAsync(schemaId.Id);
            try
            {
                var sut = new TextIndexingProcess(indexer, State);

                await action(sut);
            }
            finally
            {
                await Factory.CleanupAsync();
            }
        }
    }
}
