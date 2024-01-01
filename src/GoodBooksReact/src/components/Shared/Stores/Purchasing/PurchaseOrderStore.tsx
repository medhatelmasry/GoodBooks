﻿import {observable, extendObservable, autorun, makeObservable} from 'mobx';
import axios from "axios";
import Config from '../../Config';

import PurchaseOrder from './PurchaseOrder';
import PurchaseOrderLine from './PurchaseOrderLine';

import CommonStore from "../Common/CommonStore";

const baseUrl = location.protocol
    + "//" + location.hostname
    + (location.port && ":" + location.port)
    + "/";

export default class PurchaseOrderStore {
    purchaseOrder: PurchaseOrder;
    commonStore: CommonStore;
    validationErrors: string[] = [];
    editMode = false;
    purchaseOrderStatus: string = "";

    RTotal = 0;
    GTotal = 0;
    TTotal = 0;

    constructor(purchId: number) {
        this.commonStore = new CommonStore();
        this.purchaseOrder = new PurchaseOrder();
        extendObservable(this.purchaseOrder, {
            vendorId: this.purchaseOrder.vendorId,
            orderDate: this.purchaseOrder.orderDate,
            paymentTermId: this.purchaseOrder.paymentTermId,
            referenceNo: this.purchaseOrder.referenceNo,
            statusId: this.purchaseOrder.statusId,
            purchaseOrderLines: []
        });

        makeObservable(this, {
            validationErrors: observable,
            editMode: observable,
            purchaseOrderStatus: observable,
            RTotal: observable,
            GTotal: observable,
            TTotal: observable,
        });

        autorun(() => this.computeTotals());

        if (purchId !== undefined) {
            axios.get(Config.API_URL + "purchasing/purchaseorder?id=" + purchId)
                .then((result) => {
                    this.purchaseOrder.id = result.data.id;
                    this.purchaseOrder.paymentTermId = result.data.paymentTermId;
                    this.purchaseOrder.referenceNo = result.data.referenceNo;
                    this.purchaseOrder.statusId = result.data.statusId;
                    this.changedVendor(result.data.vendorId);
                    this.getPurchaseOrderStatus(result.data.statusId);
                    this.purchaseOrder.orderDate = result.data.orderDate;
                    for (let i = 0; i < result.data.purchaseOrderLines.length; i++) {
                        this.addLineItem(
                            result.data.purchaseOrderLines[i].id,
                            result.data.purchaseOrderLines[i].itemId,
                            result.data.purchaseOrderLines[i].measurementId,
                            result.data.purchaseOrderLines[i].quantity,
                            result.data.purchaseOrderLines[i].amount,
                            result.data.purchaseOrderLines[i].discount,
                            result.data.purchaseOrderLines[i].code // Add the missing argument
                        );
                        this.updateLineItem(i, 'code', Number(this.changeItemCode(result.data.purchaseOrderLines[i].itemId)));
                    }
                    this.computeTotals();
                    const nodes = document.getElementById("divPurchaseOrderForm")?.getElementsByTagName('*');
                    for (let i = 0; nodes && i < nodes.length; i++) {
                        nodes[i].className += " disabledControl";
                    }
                })
                .catch(() => {
                });
        }
        else {
            this.changedEditMode(true);
        }
    }

    changeItemCode(itemId: string) {
        for (let x = 0; x < this.commonStore.items.length; x++) {
            const item = this.commonStore.items[x] as PurchaseOrderLine
            if (item.id === parseInt(itemId)) {
                return item.code;
            }
        }
    }

    computeTotals() {
        let rtotal = 0;
        let ttotal = 0;

        for (let i = 0; i < this.purchaseOrder.purchaseOrderLines.length; i++) {
            const lineItem = this.purchaseOrder.purchaseOrderLines[i];
            rtotal = rtotal + this.getLineTotal(i);
            axios.get(Config.API_URL + "tax/gettax?itemId=" + lineItem.itemId + "&partyId=" + this.purchaseOrder.vendorId + "&type=2")
                .then((result) => {
                    if (result.data.length > 0) {
                        ttotal = ttotal + this.commonStore.getPurhcaseLineTaxAmount(lineItem.quantity, lineItem.amount, lineItem.discount, result.data);
                    }
                    this.TTotal = ttotal;
                    this.GTotal = rtotal + ttotal;
                });
            this.RTotal = rtotal;
        }
    }

    savePurchaseOrder() {
        if (this.purchaseOrder.orderDate === undefined)
            this.purchaseOrder.orderDate = new Date(new Date(Date.now()).toISOString().substring(0, 10));

        if (this.validation() && this.validationErrors.length === 0) {
            axios.post(Config.API_URL + "purchasing/savepurchaseorder", JSON.stringify(this.purchaseOrder),
                {
                    headers: {
                        'Content-type': 'application/json'
                    }
                })
                .then(() => {
                    window.location.href = baseUrl + 'purchasing/purchaseorders';
                })
                .catch((error) => {
                    if (axios.isAxiosError(error)) {
                        this.validationErrors.push(`Status: ${error.status} - Message: ${error.response?.data}`);
                      } else {
                        console.error(error);
                        this.validationErrors.push(`Error: ${error}`);
                      }
                })
        }
    }

    validation() {
        this.validationErrors = [];
        if (this.purchaseOrder.vendorId === undefined || this.purchaseOrder.vendorId.toString() === "")
            this.validationErrors.push("Vendor is required.");
        if (this.purchaseOrder.paymentTermId === undefined || this.purchaseOrder.paymentTermId.toString() === "")
            this.validationErrors.push("Payment term is required.");
        if (this.purchaseOrder.orderDate === undefined || this.purchaseOrder.orderDate.toString() === "")
            this.validationErrors.push("Date is required.");
        if (this.purchaseOrder.purchaseOrderLines === undefined || this.purchaseOrder.purchaseOrderLines.length < 1)
            this.validationErrors.push("Enter at least 1 line item.");
        if (this.purchaseOrder.purchaseOrderLines !== undefined && this.purchaseOrder.purchaseOrderLines.length > 0) {
            for (let i = 0; i < this.purchaseOrder.purchaseOrderLines.length; i++) {
                if (this.purchaseOrder.purchaseOrderLines[i].itemId === undefined
                    || this.purchaseOrder.purchaseOrderLines[i].itemId.toString() === "")
                    this.validationErrors.push("Item is required.");
                if (this.purchaseOrder.purchaseOrderLines[i].measurementId === undefined
                    || this.purchaseOrder.purchaseOrderLines[i].measurementId.toString() === "")
                    this.validationErrors.push("Uom is required.");
                if (this.purchaseOrder.purchaseOrderLines[i].quantity === undefined
                    || this.purchaseOrder.purchaseOrderLines[i].quantity.toString() === ""
                    || this.purchaseOrder.purchaseOrderLines[i].quantity === 0)
                    this.validationErrors.push("Quantity is required.");
                if (this.purchaseOrder.purchaseOrderLines[i].amount === undefined
                    || this.purchaseOrder.purchaseOrderLines[i].amount.toString() === ""
                    || this.purchaseOrder.purchaseOrderLines[i].amount === 0)
                    this.validationErrors.push("Amount is required.");
                if (this.getLineTotal(i) === undefined
                    || this.getLineTotal(i).toString() === "NaN"
                    || this.getLineTotal(i) === 0)
                    this.validationErrors.push("Invalid data.");
            }
        }

        return this.validationErrors.length === 0;
    }

    validationLine() {
        this.validationErrors = [];
        if (this.purchaseOrder.purchaseOrderLines !== undefined && this.purchaseOrder.purchaseOrderLines.length > 0) {
            for (let i = 0; i < this.purchaseOrder.purchaseOrderLines.length; i++) {
                if (this.purchaseOrder.purchaseOrderLines[i].itemId === undefined)
                    this.validationErrors.push("Item is required.");
                if (this.purchaseOrder.purchaseOrderLines[i].measurementId === undefined)
                    this.validationErrors.push("Uom is required.");
                if (this.purchaseOrder.purchaseOrderLines[i].quantity === undefined)
                    this.validationErrors.push("Quantity is required.");
                if (this.purchaseOrder.purchaseOrderLines[i].amount === undefined)
                    this.validationErrors.push("Amount is required.");
                if (this.getLineTotal(i) === undefined
                    || this.getLineTotal(i).toString() === "NaN")
                    this.validationErrors.push("Invalid data.");
            }
        }
        else {
            const itemId: string = (document.getElementById("optNewItemId") as HTMLInputElement).value;
            const measurementId: string = (document.getElementById("optNewMeasurementId") as HTMLInputElement).value;
            const quantity: string = (document.getElementById("txtNewQuantity") as HTMLInputElement).value;
            const amount: string = (document.getElementById("txtNewAmount") as HTMLInputElement).value;

            if (itemId == "" || itemId === undefined)
                this.validationErrors.push("Item is required.");
            if (measurementId == "" || measurementId === undefined)
                this.validationErrors.push("Uom is required.");
            if (quantity == "" || quantity === undefined)
                this.validationErrors.push("Quantity is required.");
            if (amount == "" || amount === undefined)
                this.validationErrors.push("Amount is required.");
        }

        if (document.getElementById("optNewItemId")) {

            const itemId: string = (document.getElementById("optNewItemId") as HTMLInputElement).value;
            const measurementId: string = (document.getElementById("optNewMeasurementId") as HTMLInputElement).value;
            const quantity: string = (document.getElementById("txtNewQuantity") as HTMLInputElement).value;
            const amount: string = (document.getElementById("txtNewAmount") as HTMLInputElement).value;
 
            if (itemId == "" || itemId === undefined)
                this.validationErrors.push("Item is required.");
            if (measurementId == "" || measurementId === undefined)
                this.validationErrors.push("Uom is required.");
            if (quantity == "" || quantity === undefined)
                this.validationErrors.push("Quantity is required.");
            if (amount == "" || amount === undefined)
                this.validationErrors.push("Amount is required.");
        }


        return this.validationErrors.length === 0;
    }

    changedReferenceNo(refNo: string) {
        this.purchaseOrder.referenceNo = refNo;
    }

    changedVendor(vendorId: number) {
        this.purchaseOrder.vendorId = vendorId;
    }

    changedPaymentTerm(paymentTermId: number) {
        this.purchaseOrder.paymentTermId = paymentTermId;
    }

    changedOrderDate(date: Date) {
        this.purchaseOrder.orderDate = date;
    }

    addLineItem(id: number, itemId: number, measurementId: number, quantity: number, amount: number, discount: number, code: any) {
        const newLineItem = new PurchaseOrderLine(id, itemId, measurementId, quantity, amount, discount, code);
        this.purchaseOrder.purchaseOrderLines.push(extendObservable(newLineItem, newLineItem));        
    }

    removeLineItem(row: number) {
        this.purchaseOrder.purchaseOrderLines.splice(row, 1);
    }

    updateLineItem(row: number, targetProperty: keyof PurchaseOrderLine, value: string | number) {
        if (this.purchaseOrder.purchaseOrderLines.length > 0)
            (this.purchaseOrder.purchaseOrderLines[row] as Record<keyof PurchaseOrderLine, string | number>)[targetProperty] = value;

        this.computeTotals();
    }

    // updateLineItem(row: number, targetProperty: keyof PurchaseInvoiceLine, value: string | number) {
    //     if (this.purchaseInvoice.purchaseInvoiceLines.length > 0)
    //         (this.purchaseInvoice.purchaseInvoiceLines[row] as Record<keyof PurchaseInvoiceLine, string | number>)[targetProperty] = value;

    //     this.computeTotals();
    // }

    getLineTotal(row: number) {
        let lineSum = 0;
        const lineItem = this.purchaseOrder.purchaseOrderLines[row];
        lineSum = (lineItem.quantity * lineItem.amount) - lineItem.discount;
        return lineSum;
    }

    changedEditMode(editMode: boolean) {
        this.editMode = editMode;
    }

    getPurchaseOrderStatus(statusId: number) {
        let status = "";
        if (statusId === 0)
            status = "Draft";
        else if (statusId === 1)
            status = "Open";
        else if (statusId === 2)
            status = "Partially Received";
        else if (statusId === 3)
            status = "Full Received";
        else if (statusId === 4)
            status = "Invoiced";
        else if (statusId === 5)
            status = "Closed";
        this.purchaseOrderStatus = status;
    }


}