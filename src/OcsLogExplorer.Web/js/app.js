/* UTILS - helper components and functions used all over the place */

var ResponseStatusCodeButton = React.createClass({
    render: function() {
        switch(this.props.statusCode)
        {
            case 200:
                return(<button type="button" className="btn btn-xs btn-success" alt="200">OK</button>);
            case 400:
                return(<button type="button" className="btn btn-xs btn-danger" alt="400">BadRequest</button>);
            case 404:
                return(<button type="button" className="btn btn-xs btn-danger" alt="404">BadWopiSrc</button>);
            case 405:
                return(<button type="button" className="btn btn-xs btn-danger" alt="405">BadOverride</button>);
            case 406:
                return(<button type="button" className="btn btn-xs btn-danger" alt="406">NoSupportedFormat</button>);
            case 410:
                return(<button type="button" className="btn btn-xs btn-danger" alt="410">NoSession</button>);
            case 500:
                return(<button type="button" className="btn btn-xs btn-danger" alt="500">ServerError</button>);
            case 503:
                return(<button type="button" className="btn btn-xs btn-warning" alt="503">ServerBusy</button>);
            default:
                return(<span></span>);
        }
    }
})

var ResultButton = React.createClass({
    render: function() {
        switch(this.props.result)
        {
            case "success":
                return(<button type="button" className="btn btn-xs btn-success">Success</button>);
            case "failure":
                return(<button type="button" className="btn btn-xs btn-danger">Failure</button>);
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
        console.log(this.props);
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
    getInitialState: function() {
        return {requests: []};
    },
    componentDidMount: function() {
        this.fetchData(this.props.url);
    },
    componentWillReceiveProps: function(newProps) {
        if(this.props.url != newProps.url && newProps.url)
            this.fetchData(newProps.url);
    },
    fetchData: function(url) {
        $.ajax({
            url: url,
            dataType: 'json',
            cache: false,
            success: function(data) {this.setState({requests: data});}.bind(this),
            error: function(jqXHR, status, err) {console.error(url, status, err.toString());}
        });
    },
    onAllRequestsListFilterchanged: function(filter) {
        alert(filter);
    },
    render: function() {
        let requests = this.state.requests;
        if(!requests)
            return false;

// <RequestListFilters onFilterChanged={this.onAllRequestsListFilterchanged} type="mocsi" />

        return(
            <div>
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
                        <RequestList requests={requests} />
                    </div>
                    <div role="tabpanel" className="tab-pane" id="mocsi">
                        <RequestList requests={requests} type="mocsi" />
                    </div>
                    <div role="tabpanel" className="tab-pane" id="outerLoop">
                        <RequestList requests={requests} type="outerLoop" />
                    </div>
                </div>
            </div>
        );
    }
})

var RequestListFilters = React.createClass({
    onRequestTypeFilterChanged: function(e) {

    },
    render: function() {
        let type = this.props.type;

        return(
            <div className="filterList">
                <p className="filterListRow">
                    <strong>StatusCode:</strong>
                    <label>OK</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>BadRequest</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>BadWopiSrc</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>BadOverride</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>NoSupportedFormat</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>NoSession</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>ServerError</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>ServerBusy</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                </p>
                <p className="filterListRow">
                    <strong>RequestType:</strong>
                    <label>JoinSession</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>UpdateRevision</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>GetRevision</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>PutBlobs</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>GetBlobs</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>LeaveSession</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>StartSession</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                    <label>Synchronize</label><input type="checkbox" defaultChecked="checked" className="filterListCheckbox" />
                </p>
            </div>
        );
    }
});

var RequestList = React.createClass({
    render: function() {
        let requests = this.props.requests;
        if(requests === undefined)
        return false;

        console.log(this.props);
        var id = 0;

        let getRequestType = function(method) {
            if(method === "Synchronize" || method === "StartSession")
                return "outerLoop";
            return "mocsi";
        }
        let showRequest = function(request) {
            return this.props.type === undefined || this.props.type == getRequestType(request.method)
        }.bind(this);

        var items = requests.filter(showRequest).map(function(request) {
            return(<tr key={(this.props.type) + "_" + (id++) + '_' + request.correlation}>
                    <td>{request.startTime}</td>
                    <td>{request.endTime}</td>
                    <td>{request.correlation}</td>
                    <td>{request.ocsClientSessionId}</td>
                    <td>{request.method}</td>
                    <td className="status"><ResponseStatusCodeButton statusCode={request.statusCode} /></td>
                    <td className="status"><ResultButton result={request.result} /></td>
                </tr>)
            }.bind(this));

        return(
            <div className="table-responsive">
                <table className="table table-striped">
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
                    </thead>
                    <tbody>
                        {items}
                    </tbody>
                </table>
            </div>
        );
    }
})

/* End - LISTS */

/* DETAILS - detailed views for OcsSession and OcsClientSession */ 

var OcsSession = React.createClass({
    render: function() {
        let data = this.props.ocsSessionDetails.details;

        if(!data)
            return false;

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
                <RequestLists url={data.requestsDataUrl} />
            </div>
        );
    }
});

/* End - DETAILS */

/* CONTAINER - main component which is stiching everything together and getting initial data to display */

var OcsLogExplorer = React.createClass({
    getInitialState: function() {
        return {data: [], ocsSessionDetails: {}};
    },
    componentDidMount: function() {
        let search = window.location.search;
        if(!search)
            return;

        let url = "/api/overview/" + search.substr(1);
        $.ajax({
            url: url,
            dataType: 'json',
            cache: false,
            success: function(data) {this.setState({data: data});}.bind(this),
            error: function(jqXHR, status, err) {console.error(this.props.url, status, err.toString());}.bind(this)
        });
    },
    onOcsSessionSelected: function(ocsSessionDetails) {
        this.setState({ocsSessionDetails: ocsSessionDetails});
    },
    render: function() {
        return (
            <div className="row">
                <div className="col-sm-3 col-md-2 sidebar">
                    <OcsSessionList ocsSessions={this.state.data} onOcsSessionSelected={this.onOcsSessionSelected} />
                </div>
            <OcsSession ocsSessionDetails={this.state.ocsSessionDetails} />
            </div>
        );
    }
});

/* end - CONTAINER */

ReactDOM.render(
    <OcsLogExplorer />,
    document.getElementById('OcsLogExplorer')
);