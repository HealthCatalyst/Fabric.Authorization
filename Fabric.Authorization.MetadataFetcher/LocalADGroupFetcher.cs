using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabric.Authorization.MetadataFetcher
{
    public class LocalADGroupFetcher : IGroupFetcher
    {
        public IEnumerable<string> FetchAllGroups(Dictionary<string, string> props)
        {
            // create your domain context
            var context = new PrincipalContext(ContextType.Domain);
            var qbeGroup = new GroupPrincipal(context);

            // create your principal searcher passing in the QBE principal
            var searcher = new PrincipalSearcher(qbeGroup);
            // find all matches
            foreach (var found in searcher.FindAll())
            {
                var name = found.Name;
                Console.WriteLine($"Group: {name}");
            }

            Console.ReadLine();
            return searcher.FindAll().Select(g => g.Name);
        }
    }
}
