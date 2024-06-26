﻿import * as React from "react";
import {observer} from "mobx-react";

class SelectLineMeasurement extends React.Component<any, {}>{
    onChangeMeasurement(e: any) {
        if (this.props.row !== undefined)
            this.props.store.updateLineItem(this.props.row, "measurementId", e.target.value);
    }
    render() {
        const options: JSX.Element[] = [];
        this.props.store.commonStore.measurements.map(function (measurement: any) {
            return (
                options.push(<option key={ measurement.id } value={ measurement.id } > { measurement.description } </option>)
            );
        });

        return (
            <select value={this.props.selected} id={this.props.controlId} onChange={this.onChangeMeasurement.bind(this) } className="form-control select2">
                <option key={ -1 }></option>
                {options}
            </select>
        );
    }
}
const ObservedSelectLineMeasurement = observer(SelectLineMeasurement);

export default ObservedSelectLineMeasurement;
