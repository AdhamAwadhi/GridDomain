﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automatonymous;
using Automatonymous.Activities;
using Automatonymous.Binders;
using GridDomain.CQRS;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using Shop.Domain.Aggregates.AccountAggregate.Commands;
using Shop.Domain.Aggregates.AccountAggregate.Events;
using Shop.Domain.Aggregates.OrderAggregate;
using Shop.Domain.Aggregates.OrderAggregate.Commands;
using Shop.Domain.Aggregates.OrderAggregate.Events;
using Shop.Domain.Aggregates.SkuStockAggregate.Commands;
using Shop.Domain.Aggregates.SkuStockAggregate.Events;
using Shop.Domain.Aggregates.UserAggregate;
using Shop.Domain.Aggregates.UserAggregate.Commands;
using Shop.Domain.Aggregates.UserAggregate.Events;
using Shop.Domain.DomainServices;

namespace Shop.Domain.Sagas
{
    public class BuyNowSaga : Saga<BuyNowSagaData>
    {
        public static readonly ISagaDescriptor Descriptor
            = SagaExtensions.CreateDescriptor<BuyNowSaga,
                                              BuyNowSagaData,
                                              SkuPurchaseOrdered>(new BuyNowSaga(null));

        public BuyNowSaga(IPriceCalculator calculator)
        {
             Command<CreateOrderCommand>();
             Command<AddItemToOrderCommand>();
             Command<ReserveStockCommand>();
             Command<CalculateOrderTotalCommand>();
             Command<PayForOrderCommand>();
             Command<TakeReservedStockCommand>();
             Command<CompleteOrderCommand>();
             Command<CompletePendingOrderCommand>();

             Event(() => PurchaseOrdered);
             Event(() => ItemAdded);
             Event(() => OrderCreated);
             Event(() => StockReserved);
             Event(() => OrderFinilized);
             Event(() => OrderPaid);
             Event(() => ReserveTaken);

            During(ReceivingPurchaseOrder,
                When(PurchaseOrdered).Then((state, domainEvent) =>
                {
                    state.AccountId = domainEvent.AccountId;
                    state.OrderId = domainEvent.OrderId;
                    state.Quantity = domainEvent.Quantity;
                    state.SkuId = domainEvent.SkuId;
                    state.UserId = domainEvent.SourceId;
                    state.StockId = domainEvent.StockId;

                    Dispatch(new CreateOrderCommand(state.OrderId,state.UserId));
                }).TransitionTo(CreatingOrder));

            During(CreatingOrder,
                When(OrderCreated).Then((state,e) =>
                {
                    var totalPrice = calculator.CalculatePrice(state.SkuId, state.Quantity);
                    Dispatch(new AddItemToOrderCommand(state.OrderId,
                                                       state.SkuId,
                                                       state.Quantity,
                                                       totalPrice));
                }).TransitionTo(AddingOrderItems));

            During(AddingOrderItems,
                   When(ItemAdded).Then((state,e) =>
                   {
                       Dispatch(new ReserveStockCommand(state.StockId,state.UserId,state.Quantity));
                   }).TransitionTo(Reserving));

            During(Reserving,
                   When(StockReserved).Then(ctx =>
                   {
                       Dispatch(new CalculateOrderTotalCommand(ctx.Instance.OrderId));
                   }),
                   When(OrderFinilized).Then((state, domainEvent)  =>
                   {
                       Dispatch(new PayForOrderCommand(state.AccountId,domainEvent.TotalPrice,state.OrderId));
                   })
                   .TransitionTo(Paying));

            During(Paying,
                When(OrderPaid).Then((state, e) =>
                {
                    Dispatch(new TakeReservedStockCommand(state.StockId, state.ReserveId));
                }).TransitionTo(TakingStock));

            During(TakingStock,
                When(ReserveTaken).Then((state, e) =>
                {
                    Dispatch(new CompleteOrderCommand(state.OrderId));
                    Dispatch(new CompletePendingOrderCommand(state.UserId,state.OrderId));
                }).Finalize());
        }

        public Event<SkuPurchaseOrdered> PurchaseOrdered { get; private set; }
        public Event<OrderCreated> OrderCreated { get; private set; }
        public Event<ItemAdded> ItemAdded { get; private set; }
        public Event<StockReserved> StockReserved { get; private set; }
        public Event<TotalCalculated> OrderFinilized { get; private set; }
        public Event<AccountWithdrawal> OrderPaid { get; private set; }
        public Event<StockReserveTaken> ReserveTaken { get; private set; }

        public State ReceivingPurchaseOrder{ get; private set; }
        public State CreatingOrder { get; private set; }
        public State AddingOrderItems { get; private set; }
        public State Reserving { get; private set; }
        public State Paying { get; private set; }
        public State TakingStock { get; private set; }
    }
}