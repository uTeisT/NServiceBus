﻿namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Features;
    using NServiceBus.Routing.Legacy;
    using NUnit.Framework;

    public class When_worker_uses_delayed_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_distributor_timeout_manager()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Worker>()
                .WithEndpoint<Distributor>(e => e
                    .When(s => s.Send(new DelayedMessage())))
                .Done(c => c.DistributorReceivedRetry)
                .Run();

            Assert.IsTrue(context.DistributorReceivedRetry);
        }

        class Context : ScenarioContext
        {
            public bool DistributorReceivedRetry { get; set; }
        }

        class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                var distributorAddress = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Distributor));
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.EnableFeature<FakeReadyMessageProcessor>();
                    c.EnlistWithLegacyMSMQDistributor(
                        distributorAddress,
                        "ReadyMessages",
                        10);
                    c.Recoverability()
                        .Immediate(s => s
                            .NumberOfRetries(0))
                        .Delayed(s => s
                            .NumberOfRetries(1)
                            .TimeIncrease(TimeSpan.Zero));
                });
            }

            class DelayedMessageHandler : IHandleMessages<DelayedMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DelayedMessage message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.AddHeaderToAllOutgoingMessages("NServiceBus.Distributor.WorkerSessionId", Guid.NewGuid().ToString());
                }).AddMapping<DelayedMessage>(typeof(Worker));
            }

            class DelayedMessageHandler : IHandleMessages<DelayedMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DelayedMessage message, IMessageHandlerContext context)
                {
                    testContext.DistributorReceivedRetry = true;
                    return Task.CompletedTask;
                }
            }
        }

        class DelayedMessage : ICommand
        {
        }
    }
}