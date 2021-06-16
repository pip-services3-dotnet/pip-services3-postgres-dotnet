using PipServices3.Commons.Data;
using PipServices3.Postgres.Persistence;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PipServices3.Postgres.Fixtures
{
	public class DummyPersistenceFixture2
    {
        private Dummy2 _dummy1 = new Dummy2 
        { 
            Id = 0,
            Key = "Key 1", 
            Content = "Content 1", 
            CreateTimeUtc = DateTime.UtcNow,
            SubDummy = new SubDummy { Type = "some type", ArrayOfDouble = new double[] { 10, 10 } } 
        };
        private Dummy2 _dummy2 = new Dummy2 
        {
            Id = 1,
            Key = "Key 2", 
            Content = "Content 2", 
            CreateTimeUtc = DateTime.UtcNow,
            SubDummy = new SubDummy { Type = "some type", ArrayOfDouble = new double[] { 2, 2 } }
        };

        private IDummyPersistence2 _persistence;

        public DummyPersistenceFixture2(IDummyPersistence2 persistence)
        {
            _persistence = persistence;
        }

        public async Task TestCrudOperationsAsync()
        {
            // Create one dummy
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);

            Assert.NotNull(dummy1);
            Assert.NotNull(dummy1.SubDummy);
            Assert.Equal(_dummy1.Key, dummy1.Key);
            Assert.Equal(_dummy1.Content, dummy1.Content);
            Assert.Equal(_dummy1.CreateTimeUtc, dummy1.CreateTimeUtc, TimeSpan.FromMilliseconds(1000));
            Assert.Equal(_dummy1.SubDummy.Type, dummy1.SubDummy.Type);
            Assert.Equal(_dummy1.SubDummy.ArrayOfDouble, dummy1.SubDummy.ArrayOfDouble);

            // Create another dummy
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            Assert.NotNull(dummy2);
            Assert.NotNull(dummy2.SubDummy);
            Assert.Equal(_dummy2.Key, dummy2.Key);
            Assert.Equal(_dummy2.Content, dummy2.Content);
            Assert.Equal(_dummy2.CreateTimeUtc, dummy2.CreateTimeUtc, TimeSpan.FromMilliseconds(1000));
            Assert.Equal(_dummy2.SubDummy.Type, dummy2.SubDummy.Type);
            Assert.Equal(_dummy2.SubDummy.ArrayOfDouble, dummy2.SubDummy.ArrayOfDouble);

            var page = await _persistence.GetPageByFilterAsync(null, null, null);
            Assert.NotNull(page);
            Assert.Equal(2, page.Data.Count);

            page = await _persistence.GetPageByFilterAsync(null, FilterParams.FromTuples("key", _dummy2.Key), null);
            Assert.NotNull(page);
            Assert.Single(page.Data);

            // Update the dummy
            dummy1.Content = "Updated Content 1";
            var result = await _persistence.UpdateAsync(null, dummy1);
            Assert.NotNull(result);
            Assert.NotNull(result.SubDummy);
            Assert.Equal(dummy1.Id, result.Id);
            Assert.Equal(dummy1.Key, result.Key);
            Assert.Equal(dummy1.Content, result.Content);
            Assert.Equal(dummy1.CreateTimeUtc, result.CreateTimeUtc, TimeSpan.FromMilliseconds(1000));
            Assert.Equal(dummy1.SubDummy.Type, result.SubDummy.Type);
            Assert.Equal(dummy1.SubDummy.ArrayOfDouble, result.SubDummy.ArrayOfDouble);

            // Set the dummy
            dummy1.Content = "Updated Content 2";
            result = await _persistence.SetAsync(null, dummy1);
            Assert.NotNull(result);
            Assert.NotNull(result.SubDummy);
            Assert.Equal(dummy1.Id, result.Id);
            Assert.Equal(dummy1.Key, result.Key);
            Assert.Equal(dummy1.Content, result.Content);
            Assert.Equal(dummy1.CreateTimeUtc, result.CreateTimeUtc, TimeSpan.FromMilliseconds(1000));
            Assert.Equal(dummy1.SubDummy.Type, result.SubDummy.Type);
            Assert.Equal(dummy1.SubDummy.ArrayOfDouble, result.SubDummy.ArrayOfDouble);

            // Partially update the dummy
            result = await _persistence.UpdatePartially(null, dummy1.Id,
                AnyValueMap.FromTuples("content", "Partially Updated Content 1"));
            Assert.NotNull(result);
            Assert.Equal(dummy1.Id, result.Id);
            Assert.Equal(dummy1.Key, result.Key);
            Assert.Equal("Partially Updated Content 1", result.Content);

            // Get the dummy by Id
            result = await _persistence.GetOneByIdAsync(null, dummy1.Id);
            Assert.NotNull(result);
            Assert.Equal(dummy1.Id, result.Id);
            Assert.Equal(dummy1.Key, result.Key);

            // Delete the dummy
            result = await _persistence.DeleteByIdAsync(null, dummy1.Id);
            Assert.NotNull(result);
            Assert.Equal(dummy1.Id, result.Id);
            Assert.Equal(dummy1.Key, result.Key);

            // Get the deleted dummy
            result = await _persistence.GetOneByIdAsync(null, dummy1.Id);
            Assert.Null(result);

            var count = await _persistence.GetCountByFilterAsync(null, null);
            Assert.Equal(1, count);
        }

        public async Task TestBatchOperationsAsync()
        {
            // Create one dummy
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);

            Assert.NotNull(dummy1);
            Assert.NotNull(dummy1.Id);
            Assert.Equal(_dummy1.Key, dummy1.Key);
            Assert.Equal(_dummy1.Content, dummy1.Content);
            Assert.Equal(_dummy1.CreateTimeUtc, dummy1.CreateTimeUtc, TimeSpan.FromMilliseconds(1000));

            // Create another dummy
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            Assert.NotNull(dummy2);
            Assert.NotNull(dummy2.Id);
            Assert.Equal(_dummy2.Key, dummy2.Key);
            Assert.Equal(_dummy2.Content, dummy2.Content);
            Assert.Equal(_dummy2.CreateTimeUtc, dummy2.CreateTimeUtc, TimeSpan.FromMilliseconds(1000));

            // Read batch
            var items = await _persistence.GetListByIdsAsync(null, new[] { dummy1.Id, dummy2.Id });
            Assert.NotNull(items);
            Assert.Equal(2, items.Count);

            // Delete batch
            await _persistence.DeleteByIdsAsync(null, new[] { dummy1.Id, dummy2.Id });

            // Read empty batch
            items = await _persistence.GetListByIdsAsync(null, new[] { dummy1.Id, dummy2.Id });
            Assert.NotNull(items);
            Assert.Empty(items);
        }

    }
}
