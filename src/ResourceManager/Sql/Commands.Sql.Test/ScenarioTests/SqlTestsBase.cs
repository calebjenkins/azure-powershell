﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.Common.Authentication;
using Microsoft.Azure.Commands.ScenarioTest.Mocks;
using Microsoft.Azure.Graph.RBAC;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Test;
using Microsoft.Azure.Test.HttpRecorder;
using Microsoft.WindowsAzure.Commands.Common;
using Microsoft.WindowsAzure.Commands.ScenarioTest;
using Microsoft.WindowsAzure.Commands.Test.Utilities.Common;
using Microsoft.WindowsAzure.Management.Storage;
using System;
using System.Collections.Generic;


namespace Microsoft.Azure.Commands.ScenarioTest.SqlTests
{
    public class SqlTestsBase : RMTestBase
    {
        protected SqlEvnSetupHelper helper;

        private const string TenantIdKey = "TenantId";
        private const string DomainKey = "Domain";

        public string UserDomain { get; private set; }

        protected SqlTestsBase()
        {
            helper = new SqlEvnSetupHelper();
        }

        protected virtual void SetupManagementClients()
        {
            var sqlCSMClient = GetSqlClient();
            var storageClient = GetStorageClient();
            //TODO, Remove the MockDeploymentFactory call when the test is re-recorded
            var resourcesClient = MockDeploymentClientFactory.GetResourceClient(GetResourcesClient());
            var authorizationClient = GetAuthorizationManagementClient();
            var graphClient = GetGraphClient();
            helper.SetupSomeOfManagementClients(sqlCSMClient, storageClient, resourcesClient, authorizationClient, graphClient);
        }
        
        protected void RunPowerShellTest(params string[] scripts)
        {
            HttpMockServer.Matcher = new PermissiveRecordMatcher();
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("Microsoft.Resources", null);
            d.Add("Microsoft.Features", null);
            d.Add("Microsoft.Authorization", null);
            var providersToIgnore = new Dictionary<string, string>();
            providersToIgnore.Add("Microsoft.Azure.Graph.RBAC.GraphRbacManagementClient", "1.42-previewInternal");
            providersToIgnore.Add("Microsoft.Azure.Management.Resources.ResourceManagementClient", "2016-02-01");
            HttpMockServer.Matcher = new PermissiveRecordMatcherWithApiExclusion(true, d, providersToIgnore);
            // Enable undo functionality as well as mock recording
            using (UndoContext context = UndoContext.Current)
            {
                // Configure recordings
                context.Start(TestUtilities.GetCallingClass(2), TestUtilities.GetCurrentMethodName(2));

                SetupManagementClients();

                helper.SetupEnvironment();

                helper.SetupModules(AzureModule.AzureResourceManager,
                    "ScenarioTests\\Common.ps1",
                    "ScenarioTests\\" + this.GetType().Name + ".ps1",
                    helper.RMProfileModule,
                    helper.RMResourceModule,
                    helper.RMStorageDataPlaneModule,
                    helper.GetRMModulePath(@"AzureRM.Insights.psd1"),
                    helper.GetRMModulePath(@"AzureRM.Sql.psd1"),
                    "AzureRM.Storage.ps1",
                    "AzureRM.Resources.ps1");
                helper.RunPowerShellTest(scripts);
            }
        }

        protected SqlManagementClient GetSqlClient()
        {
            SqlManagementClient client = TestBase.GetServiceClient<SqlManagementClient>(new CSMTestEnvironmentFactory());
            if (HttpMockServer.Mode == HttpRecorderMode.Playback)
            {
                client.LongRunningOperationInitialTimeout = 0;
                client.LongRunningOperationRetryTimeout = 0;
            }
            return client;
        }

        protected StorageManagementClient GetStorageClient()
        {
            StorageManagementClient client = TestBase.GetServiceClient<StorageManagementClient>(new RDFETestEnvironmentFactory());
            if (HttpMockServer.Mode == HttpRecorderMode.Playback)
            {
                client.LongRunningOperationInitialTimeout = 0;
                client.LongRunningOperationRetryTimeout = 0;
            }
            return client;
        }

        protected ResourceManagementClient GetResourcesClient()
        {
            ResourceManagementClient client = TestBase.GetServiceClient<ResourceManagementClient>(new CSMTestEnvironmentFactory());
            if (HttpMockServer.Mode == HttpRecorderMode.Playback)
            {
                client.LongRunningOperationInitialTimeout = 0;
                client.LongRunningOperationRetryTimeout = 0;
            }
            return client;
        }

        protected AuthorizationManagementClient GetAuthorizationManagementClient()
        {
            AuthorizationManagementClient client = TestBase.GetServiceClient<AuthorizationManagementClient>(new CSMTestEnvironmentFactory());
            if (HttpMockServer.Mode == HttpRecorderMode.Playback)
            {
                client.LongRunningOperationInitialTimeout = 0;
                client.LongRunningOperationRetryTimeout = 0;
            }
            return client;
        }

        protected GraphRbacManagementClient GetGraphClient()
        {
            var testFactory = new CSMTestEnvironmentFactory();
            var environment = testFactory.GetTestEnvironment();
            string tenantId = Guid.Empty.ToString();

            if (HttpMockServer.Mode == HttpRecorderMode.Record)
            {
                tenantId = environment.AuthorizationContext.TenantId;
                UserDomain = environment.AuthorizationContext.UserDomain;

                HttpMockServer.Variables[TenantIdKey] = tenantId;
                HttpMockServer.Variables[DomainKey] = UserDomain;
            }
            else if (HttpMockServer.Mode == HttpRecorderMode.Playback)
            {
                if (HttpMockServer.Variables.ContainsKey(TenantIdKey))
                {
                    tenantId = HttpMockServer.Variables[TenantIdKey];
                    AzureRmProfileProvider.Instance.Profile.Context.Tenant.Id = new Guid(tenantId);
                }
                if (HttpMockServer.Variables.ContainsKey(DomainKey))
                {
                    UserDomain = HttpMockServer.Variables[DomainKey];
                    AzureRmProfileProvider.Instance.Profile.Context.Tenant.Domain = UserDomain;
                }
            }

            return TestBase.GetGraphServiceClient<GraphRbacManagementClient>(testFactory, tenantId);
        }

        protected Management.Storage.StorageManagementClient GetStorageV2Client()
        {
            var client =
                TestBase.GetServiceClient<Management.Storage.StorageManagementClient>(new CSMTestEnvironmentFactory());

            if (HttpMockServer.Mode == HttpRecorderMode.Playback)
            {
                client.LongRunningOperationInitialTimeout = 0;
                client.LongRunningOperationRetryTimeout = 0;
            }
            return client;
        }
    }
}
