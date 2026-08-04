import "../../styles/dashboard.css";

function DashboardPage() {

  return (

    <div>
      <h2 className="page-title mb-4">
        Dashboard
      </h2>
      <div className="row">
        <div className="col-md-3">
          <div className="card-box">
            <h6>Total Sales</h6>
            <h3>₹25,650</h3>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card-box">
            <h6>Purchase</h6>
            <h3>₹18,400</h3>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card-box">
            <h6>Profit</h6>
            <h3>₹7,250</h3>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card-box">
            <h6>Products</h6>
            <h3>235</h3>
          </div>
        </div>
      </div>
    </div>
  );
}

export default DashboardPage;