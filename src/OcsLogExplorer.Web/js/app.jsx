var OcsSessionList = React.createClass({
    handleOcsSessionSelectionChange: function(ocsClientSessionIds) {
        this.props.onOcsSessionSelected(ocsClientSessionIds);
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
                                   onClick={function(e) { this.handleOcsSessionSelectionChange(ocsSessionDetails.ocsClientSessionIds); }.bind(this)}>
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
        alert(ocsClientSessionId);
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

var OcsLogExplorer = React.createClass({
  getInitialState: function() {
    return {data: [], ocsClientSessionIds: []};
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
  onOcsSessionSelected: function(ocsClientSessionIds) {
      this.setState({ocsClientSessionIds: ocsClientSessionIds})
      console.log(ocsClientSessionIds);
  },
  render: function() {
      return (
          <div className="row">
            <div className="col-sm-3 col-md-2 sidebar">
                <OcsSessionList data={this.state.data} onOcsSessionSelected={this.onOcsSessionSelected} />
                <OcsClientSessionList ocsClientSessionIds={this.state.ocsClientSessionIds} />
            </div>
          </div>
      );
    }
});

ReactDOM.render(
  <OcsLogExplorer url="data.json" />,
  document.getElementById('OcsLogExplorer')
);