/* UTILS - helper components and functions used all over the place */

function getRequestType(method) {
    if(method === "Synchronize" || method === "StartSession")
        return "outerLoop";
    return "mocsi";
}

function formatDate(dateStr) {
    if(dateStr === undefined)
        return;
    return new Date(dateStr).toLocaleString()
}

var Loading = React.createClass({
    render: function() {
        return(<p className="loading"><span className="glyphicon glyphicon-refresh glyphicon-refresh-animate">
  </span> Loading ...</p>);
    }
})

var ResponseStatusCodeButton = React.createClass({
    render: function() {
        switch(this.props.statusCode)
        {
            case 200:
                return(<span className="label label-success" alt="200">OK</span>);
            case 400:
                return(<span className="label label-danger" alt="400">BadRequest</span>);
            case 404:
                return(<span className="label label-danger" alt="404">BadWopiSrc</span>);
            case 405:
                return(<span className="label label-danger" alt="405">BadOverride</span>);
            case 406:
                return(<span className="label label-danger" alt="406">NoSupportedFormat</span>);
            case 410:
                return(<span className="label label-danger" alt="410">NoSession</span>);
            case 500:
                return(<span className="label label-danger" alt="500">ServerError</span>);
            case 503:
                return(<span className="label label-warning" alt="503">ServerBusy</span>);
            default:
                return(<span></span>);
        }
    }
})

var ResultButton = React.createClass({
    render: function() {
        switch(this.props.result)
        {
            case true:
                return(<span className="label label-success">Success</span>);
            case false:
                return(<span className="label label-danger">Failure</span>);
            default:
                return(<span></span>);
        }
    }
})

/* End - UTILS */

/* LISTS
    - OcsSessionList and OcsClientSessionList components rendered on the left-hand side of the UI
    - SynchronizeRequestList rendered in details view.
 */

var OcsSessionList = React.createClass({
    handleOcsSessionSelectionChange: function(ocsSessionDetails) {
        this.props.onOcsSessionSelected(ocsSessionDetails);
    },
    render: function() {
        if(!this.props || !this.props.ocsSessions || this.props.ocsSessions.length == 0)
        {
            return false;
        }
        else
        {
            let items = this.props.ocsSessions.map(function(ocsSessionDetails) {
                        return(<li key={ocsSessionDetails.ocsSessionId}
                                   onClick={function(e) { this.handleOcsSessionSelectionChange(ocsSessionDetails); }.bind(this)}>
                            <a href="#">{ocsSessionDetails.ocsSessionId}</a>
                        </li>)
                    }.bind(this));
            return (
                <ul className="nav nav-sidebar" id="OcsSessionList">
                    <li><a className="header">OCS Sessions</a></li>
                    {items}
                </ul>
            )
        }
    }
});

var OcsClientSessionList = React.createClass({
    render: function() {
        if(this.props.ocsClientSessionIds === undefined)
        {
            return false;
        }
        else
        {
            let items = this.props.ocsClientSessionIds.map(function(ocsClientSessionId) {
                        return(<li key={ocsClientSessionId}>{ocsClientSessionId}</li>)
                    }.bind(this));
            return (
                <div className="col-xs-6 col-lg-3 infobox">
                    <h3>OCS Client Sessions</h3>
                    <ul>
                        {items}
                    </ul>
                </div>
            )
        }
    }
});

var RequestLists = React.createClass({
    onFilterChanged: function(filter) {
        this.setState({filters: filter});
    },
    getInitialState: function() {
        return {requests: [], filters: {}};
    },
    componentDidMount: function() {
        this.fetchData(this.props.url);
    },
    componentWillReceiveProps: function(newProps) {
        if(this.props.url != newProps.url && newProps.url)
            this.fetchData(newProps.url);
    },
    fetchData: function(url) {
        this.setState({loading: true});
        $.ajax({
            url: url,
            dataType: 'json',
            cache: false,
            success: function(data) {this.setState({loading: false, requests: data});}.bind(this),
            error: function(jqXHR, status, err) {this.setState({loading: false});console.error(url, status, err.toString());}
        });
    },
    onAllRequestsListFilterchanged: function(filter) {
        alert(filter);
    },
    filterRequests: function(requests) {
        var filters = this.state.filters;

        if(!filters)
            return requests;

        var filterRequest = function(request) {
            var requestType = getRequestType(request.method);
            if(filters.hideOkSuccess 
                && ((requestType == "outerLoop" && request.statusCode == 200 && request.result)
                    || (requestType == "mocsi" && request.statusCode == 200)))
                    return false;

            return true;
        }
        return requests.filter(filterRequest);
    },
    render: function() {
        if(this.state.loading)
            return(<Loading />);

        let requests = this.state.requests;
        if(!requests)
            return false;

        requests = this.filterRequests(requests);

        return(
            <div className="requestsContainer">
                <RequestListFilters onFilterChanged={this.onFilterChanged} />
                <ul className="nav nav-tabs" role="tablist">
                    <li role="presentation" className="active">
                        <a href="#all" id="all-tab" aria-controls="all" role="tab" data-toggle="tab">All</a>
                    </li>
                    <li role="presentation">
                        <a href="#mocsi" id="mocsi-tab" aria-controls="mocsi" role="tab" data-toggle="tab">MOCSI</a>
                    </li>
                    <li role="presentation">
                        <a href="#outerLoop" id="outerLoop-tab" aria-controls="outerLoop" role="tab" data-toggle="tab">OuterLoop</a>
                    </li>
                </ul>
                <div className="tab-content">
                    <div role="tabpanel" className="tab-pane active" id="all">
                        <RequestList requests={requests} onShowRequestDetails={this.props.onShowRequestDetails} />
                    </div>
                    <div role="tabpanel" className="tab-pane" id="mocsi">
                        <RequestList requests={requests} onShowRequestDetails={this.props.onShowRequestDetails} type="mocsi" />
                    </div>
                    <div role="tabpanel" className="tab-pane" id="outerLoop">
                        <RequestList requests={requests} onShowRequestDetails={this.props.onShowRequestDetails} type="outerLoop" />
                    </div>
                </div>
            </div>
        );
    }
})

var RequestListFilters = React.createClass({
    hideOkSuccessChanged: function(e) {
        var currentFilter = {
            hideOkSuccess: e.target.className.split(' ').indexOf("active") > -1
        }
        this.props.onFilterChanged(currentFilter);
    },
    render: function() {
        let type = this.props.type;

        return(
            <div className="requestsFilterList">
                <button onClick={function(e) {this.hideOkSuccessChanged(e);}.bind(this)} className="btn btn-warning" data-toggle="button" aria-pressed="false" autocomplete="off">Hide OK/Success</button>
            </div>
        );
    }
});

var RequestListHeaderRow = React.createClass({
    render: function() {
    return(
        <thead>
            <tr>
                <th>StartTime</th>
                <th>EndTime</th>
                <th>Correlation</th>
                <th>OcsClientSessionId</th>
                <th>Method</th>
                <th className="status">StatusCode</th>
                <th className="status">Result</th>
            </tr>
        </thead>);
    }
})

var RequestListBodyRow = React.createClass({
    render: function() {
        return(
            <tr key={this.props.type + "_" + this.props.id + '_' + this.props.request.correlation}
                onClick={function(e) {
                    if(this.props.detailsOnClick)
                        this.props.onShowRequestDetails(this.props.request);}.bind(this)
                }
                className={this.props.detailsOnClick ? "detailsOnClick" : ""}>
                <td>{formatDate(this.props.request.startTime)}</td>
                <td>{formatDate(this.props.request.endTime)}</td>
                <td>{this.props.request.correlation}</td>
                <td>{this.props.request.ocsClientSessionId}</td>
                <td>{this.props.request.method}</td>
                <td className="status"><ResponseStatusCodeButton statusCode={this.props.request.statusCode} /></td>
                <td className="status"><ResultButton result={this.props.request.result} /></td>
            </tr>)
    }
})

var RequestList = React.createClass({
    render: function() {
        let requests = this.props.requests;
        if(requests === undefined)
        return false;

        let showRequest = function(request) {
            return this.props.type === undefined || this.props.type == getRequestType(request.method)
        }.bind(this);

        var items = requests.filter(showRequest).map(function(request, id) {
                return(<RequestListBodyRow detailsOnClick={true} id={id} type={this.props.type} request={request} onShowRequestDetails={this.props.onShowRequestDetails} />);
            }.bind(this));

        return(
            <div className="table-responsive">
                <table className="table table-striped">
                    <RequestListHeaderRow />
                    <tbody>
                        {items}
                    </tbody>
                </table>
            </div>
        );
    }
})

var UlsListFilters = React.createClass({
    warningsAndAbove: function(e) {
        var currentFilter = {
            warningsAndAbove: e.target.className.split(' ').indexOf("active") > -1
        }
        this.props.onFilterChanged(currentFilter);
    },
    render: function() {
        let type = this.props.type;

        return(
            <div className="ulsFilterList">
                <button onClick={function(e) {this.warningsAndAbove(e);}.bind(this)} className="btn btn-warning" data-toggle="button" aria-pressed="false" autocomplete="off">Show warnings and above only</button>
            </div>
        );
    }
});

var UlsList = React.createClass({
    onFilterChanged: function(filter) {
        this.setState({filters: filter});
    },
    getUlsClassName: function(level) {
        if(level == "Medium")
            return "";
        if(level == "Monitorable")
            return "ulsWarning";
        return "ulsError";
    },
    getInitialState: function() {
        return {logs: [], filters: {}};
    },
    componentDidMount: function() {
        this.fetchData(this.props.url);
    },
    componentWillReceiveProps: function(newProps) {
        if(this.props.url != newProps.url && newProps.url)
            this.fetchData(newProps.url);
    },
    fetchData: function(url) {
        this.setState({loading: true});
        $.ajax({
            url: url,
            dataType: 'json',
            cache: false,
            success: function(data) {this.setState({loading: false, uls: data});}.bind(this),
            error: function(jqXHR, status, err) {this.setState({loading: false});console.error(url, status, err.toString());}
        });
    },
    filterUls: function(uls) {
        let filters = this.state.filters;
        if(!filters)
            return uls;

        return uls.filter(function(log) {
            if(filters.warningsAndAbove && log.level.__Case == "Medium")
                return false;
            return true; });
    },
    render: function() {
        if(this.state.loading)
            return(<Loading />);

        let uls = this.state.uls;
        if(!uls)
            return false;
        
        uls = this.filterUls(uls);

        var logs = uls.map(function(log, id) {
                return(
                    <tr className={this.getUlsClassName(log.level.__Case)}>
                        <td>{log.timeStamp}</td>
                        <td>{log.process}</td>
                        <td>{log.thread}</td>
                        <td>{log.category}</td>
                        <td>{log.eventID}</td>
                        <td>{log.level.__Case}</td>
                        <td>{log.message}</td>
                    </tr>);
            }.bind(this));

        return(
            <div className="ulsContainer">
                <UlsListFilters onFilterChanged={this.onFilterChanged} />
                <h2 className="sub-header">ULS logs</h2>
                <div className="table-responsive">
                    <table className="table table-striped table-uls">
                        <thead>
                            <tr>
                                <th>TimeStamp</th>
                                <th>Process</th>
                                <th>Thread</th>
                                <th>Category</th>
                                <th>EventID</th>
                                <th>Level</th>
                                <th>Message</th>
                            </tr>
                        </thead>
                        <tbody>
                            {logs}
                        </tbody>
                    </table>
                </div>
            </div>);
    }
})

/* End - LISTS */

/* DETAILS - detailed views for OcsSession and OcsClientSession */ 

var OcsSession = React.createClass({
    onShowRequestDetails(request) {
        this.props.onShowRequestDetails(this.props.ocsSessionDetails.ocsSessionId, request)
    },
    render: function() {
        let data = this.props.ocsSessionDetails.details;
        if(!data)
            return false;

        let search = window.location.search;
        if(!search)
            return;

        let requestsDataUrl = "/api/requests/" + search.substr(1) + "/" + this.props.ocsSessionDetails.ocsSessionId;

        return(
            <div className="col-sm-9 col-sm-offset-3 col-md-10 col-md-offset-2 main"> 
                <h1 className="page-header">OCSSessionID - {this.props.ocsSessionDetails.ocsSessionId}</h1>
                <div className="row">
                    <div className="col-xs-6 col-lg-3 infobox">
                        <h3>Overview</h3>
                        <ul>
                            <li><strong>Application:</strong> {data.application}</li>
                            <li><strong>Environment:</strong> {data.environment}</li>
                            <li><strong>Datacenter:</strong> {data.datacenter}</li>
                            <li><strong>StartTime:</strong> {data.startTime}</li>
                            <li><strong>EndTime:</strong> {data.endTime}</li>
                        </ul>
                    </div>
                    <OcsClientSessionList ocsClientSessionIds={data.ocsClientSessionIds} />
                </div>
                <h2 className="sub-header">Requests</h2>
                <RequestLists url={requestsDataUrl} onShowRequestDetails={this.onShowRequestDetails} />
            </div>
        );
    }
});

var RequestDetailsDialog = React.createClass({
    render: function() {
        if(!this.props.requestDetails || !this.props.requestDetails.request)
            return (<div className="modal fade" id="ulsModal" tabindex="-1" role="dialog" aria-labelledby="ulsModal"></div>);

        let search = window.location.search;
        if(!search)
            return;

        let ulsUrl = "/api/uls/" + search.substr(1) + "/" + this.props.requestDetails.request.correlation;

        return(<div className="modal fade" id="ulsModal" tabindex="-1" role="dialog" aria-labelledby="ulsModal">
                <div className="modal-dialog" role="document">
                    <div className="modal-content">
                        <div className="modal-header">
                            <button type="button" className="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                            <h4 className="modal-title" id="ulsModal">{this.props.requestDetails.request.method} request - {this.props.requestDetails.request.correlation}</h4>
                        </div>
                        <div className="modal-body">
                            <h2 className="sub-header">Overview</h2>
                            <div className="table-responsive">
                                <table className="table table-striped">
                                    <RequestListHeaderRow />
                                    <tbody>
                                        <RequestListBodyRow detailsOnClick={false} id="details" type="details" request={this.props.requestDetails.request}/>
                                    </tbody>
                                </table>
                            </div>
                            <UlsList url={ulsUrl} />
                        </div>
                    </div>
                </div>
            </div>);
    }
});

/* End - DETAILS */

/* CONTAINER - main component which is stiching everything together and getting initial data to display */

var OcsLogExplorer = React.createClass({
    onShowRequestDetails(ocsSessionId, request) {
        this.setState({requestDetails: { ocsSessionId: ocsSessionId, request: request }});
        $('#ulsModal').modal();
    },
    getInitialState: function() {
        return {data: [], ocsSessionDetails: {}, requestDetails: {}};
    },
    componentDidMount: function() {
        let search = window.location.search;
        if(!search)
            return;

        let url = "/api/overview/" + search.substr(1);
        this.setState({loading: true});

        $.ajax({
            url: url,
            dataType: 'json',
            cache: false,
            success: function(data) {this.setState({loading: false, data: data});}.bind(this),
            error: function(jqXHR, status, err) {console.error(this.props.url, status, err.toString());}.bind(this)
        });
    },
    onOcsSessionSelected: function(ocsSessionDetails) {
        this.setState({ocsSessionDetails: ocsSessionDetails});
    },
    render: function() {
        if(this.state.loading)
            return(<Loading />);

        return (
            <div className="row">
                <div className="col-sm-3 col-md-2 sidebar">
                    <OcsSessionList ocsSessions={this.state.data} onOcsSessionSelected={this.onOcsSessionSelected} />
                </div>
            <OcsSession ocsSessionDetails={this.state.ocsSessionDetails} onShowRequestDetails={this.onShowRequestDetails} />
            <RequestDetailsDialog requestDetails={this.state.requestDetails} />
            </div>
        );
    }
});

/* end - CONTAINER */

ReactDOM.render(
    <OcsLogExplorer />,
    document.getElementById('OcsLogExplorer')
);