/* UTILS - helper components and functions used all over the place */

var ResponseStatusCodeButton = React.createClass({
    onClick: function(e) {

    },
    render: function() {
        let statusCode = this.props.statusCode;
        switch(statusCode)
        {
            case 200:
                return(<button type="button" className="btn btn-xs btn-success" alt="200">OK</button>);
            case 500:
                return(<button type="button" className="btn btn-xs btn-danger" alt="500">ServerError</button>);
            default:
                return(<span></span>);
        }
    }

})

/* End - UTILS */

/* LISTS - OcsSessionList and OcsClientSessionList components rendered on the left-hand side of the UI */

var OcsSessionList = React.createClass({
    handleOcsSessionSelectionChange: function(ocsSessionDetails) {
        this.props.onOcsSessionSelected(ocsSessionDetails);
    },
    render: function() {
        if(this.props.data.ocsSessions === undefined)
        {
            return (
                <ul className="nav nav-sidebar" id="OcsSessionList">
                    <li><a className="header">OCS Sessions</a></li>
                </ul>
            )
        }
        else
        {
            let items = this.props.data.ocsSessions.map(function(ocsSessionDetails) {
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
    handleOcsClientSessionSelectionChange: function(ocsClientSessionId) {
        console.log("selected OcsClientSessionId: " + ocsClientSessionId);
    },
    render: function() {
        if(this.props.ocsClientSessionIds === undefined)
        {
            return (
                <ul className="nav nav-sidebar" id="OcsClientSessionList">
                    <li><a className="header">OCS Client Sessions</a></li>
                </ul>
            )
        }
        else
        {
            let items = this.props.ocsClientSessionIds.map(function(ocsClientSessionId) {
                        return(<li key={ocsClientSessionId}
                                   onClick={function(e) { this.handleOcsClientSessionSelectionChange(ocsClientSessionId); }.bind(this)}>
                            <a href="#">{ocsClientSessionId}</a>
                        </li>)
                    }.bind(this));
            return (
                <ul className="nav nav-sidebar" id="OcsSessionList">
                    <li><a className="header">OCS Client Sessions</a></li>
                    {items}
                </ul>
            )
        }
    }
});

/* End - LISTS */

/* DETAILS - detailed views for OcsSession and OcsClientSession */ 

var OcsSession = React.createClass({
    render: function() {
        let data = this.props.ocsSessionDetails.details;
        return(
            <div className="col-sm-9 col-sm-offset-3 col-md-10 col-md-offset-2 main"> 
                <h1 className="page-header">OCSSessionID - {this.props.ocsSessionDetails.ocsSessionId}</h1>
                <div className="row">
                    <div className="col-xs-6 col-lg-4 infobox">
                        <h3>Overview</h3>
                        <ul>
                            <li><strong>StartTime:</strong> {data.overview.startTime}</li>
                            <li><strong>EndTime:</strong> {data.overview.endTime}</li>
                            <li><strong>Environment:</strong> {data.overview.environment}</li>
                            <li><strong>Datacenter:</strong> {data.overview.datacenter}</li>
                            <li><strong>Application:</strong> {data.overview.application}</li>
                        </ul>
                    </div>
                    <div className="col-xs-6 col-lg-4 infobox">
                        <h3>StartSession</h3>
                        <ul>
                            <li><strong>Request:</strong> {data.startSessionRequestDetails.requestTime}</li>
                            <li><strong>Response:</strong> {data.startSessionRequestDetails.responseTime}</li>
                            <li><strong>OL Endpoint:</strong> {data.startSessionRequestDetails.olEndpoint}</li>
                            <li><strong>StatusCode:</strong> <ResponseStatusCodeButton statusCode={data.startSessionRequestDetails.statusCode} /></li>
                            <li><strong>OuterLoop OcsClientSessionID:</strong><br /><a href="">{data.startSessionRequestDetails.olOcsClientSessionId}</a></li>
                        </ul>
                    </div>
                </div>
                </div>
                );
    }
});

/* End - DETAILS */

/* CONTAINER - main component which is stiching everything together and getting initial data to display */

var OcsLogExplorer = React.createClass({
  getInitialState: function() {
    return {data: [], ocsSessionDetails: { details: { overview: {}, startSessionRequestDetails: {}}}};
  },
  componentDidMount: function() {
      $.ajax({
          url: this.props.url,
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
                <OcsSessionList data={this.state.data} onOcsSessionSelected={this.onOcsSessionSelected} />
                <OcsClientSessionList ocsClientSessionIds={this.state.ocsSessionDetails.details.ocsClientSessionIds} />
            </div>
            <OcsSession ocsSessionDetails={this.state.ocsSessionDetails} />
          </div>
      );
    }
});

/* end - CONTAINER */

ReactDOM.render(
  <OcsLogExplorer url="data.json" />,
  document.getElementById('OcsLogExplorer')
);