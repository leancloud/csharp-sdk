using NUnit.Framework;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace Storage.Test {
    public class FullTextSearchTest : BaseTest {
        [Test]
        public async Task QueryByOrder() {
            LCSearchQuery<Account> query = new LCSearchQuery<Account>("Account");
            query.QueryString("*")
                .OrderByDescending("balance")
                .Limit(200);
            LCSearchResponse<Account> response = await query.Find();
            Assert.Greater(response.Hits, 0);
            for (int i = 0; i < response.Results.Count - 1; i++) {
                int b1 = response.Results[i].Balance;
                int b2 = response.Results[i + 1].Balance;
                Assert.GreaterOrEqual(b1, b2);
            }
        }

        [Test]
        public async Task QueryBySort() {
            LCSearchQuery<Account> query = new LCSearchQuery<Account>("Account");
            LCSearchSortBuilder sortBuilder = new LCSearchSortBuilder();
            sortBuilder.OrderByAscending("balance");
            query.QueryString("*")
                .SortBy(sortBuilder)
                .Limit(200);
            LCSearchResponse<Account> response = await query.Find();
            Assert.Greater(response.Hits, 0);
            for (int i = 0; i < response.Results.Count - 1; i++) {
                int b1 = response.Results[i].Balance;
                int b2 = response.Results[i + 1].Balance;
                Assert.LessOrEqual(b1, b2);
            }
        }
    }
}
