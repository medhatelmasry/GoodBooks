﻿import * as React from "react";
import {observer} from "mobx-react";

class SelectVendor extends React.Component<any>{
    onChangeVendor(e: any) {
        this.props.store.changedVendor(e.target.value);

        for (let i = 0; i < this.props.store.commonStore.vendors.length; i++) {
            if (this.props.store.commonStore.vendors[i].id === parseInt(e.target.value)) {
   
                this.props.store.changedPaymentTerm(this.props.store.commonStore.vendors[i].paymentTermId);
            }
            else {
                this.props.store.changedPaymentTerm("");
            }
        }

    }
    render() {
        const options: JSX.Element[] = [];
        this.props.store.commonStore.vendors.map(function (vendor: any) {
            return (
            options.push(<option key={ vendor.id } value={ vendor.id }> { vendor.name }</option>)
            );
        });

        return (
            <select id="optVendor" value={this.props.selected} onChange={this.onChangeVendor.bind(this) } className="form-control select2">
                <option key={ -1 }></option>
                {options}
            </select>
        );
    }
}
const ObservedSelectVendor = observer(SelectVendor);

export default ObservedSelectVendor;
