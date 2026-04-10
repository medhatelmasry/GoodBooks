import { createBrowserRouter } from "react-router-dom";
import App from "../App";
import JournalEntry from "../components/Financials/JournalEntry"
import ObservedPurchaseInvoice from "../components/Purchasing/PurchaseInvoice";
import ObservedAddPurchaseOrder from "../components/Purchasing/PurchaseOrder";
import ObservedSalesInvoice from "../components/Sales/SalesInvoice";
import ObservedSalesOrder from "../components/Sales/SalesOrder";
import ObservedRoleList from "../components/Administration/RoleList";
import ObservedUserList from "../components/Administration/UserList";


export const router = createBrowserRouter([
    {
        path: "/",
        element: <App />,
        children: [
            {
                path: "/journal-entry",
                element: <JournalEntry />
            },
            {
                path: "/purchasing-invoice",
                element: <ObservedPurchaseInvoice />
            },
            {
                path: "/purchase-order",
                element: <ObservedAddPurchaseOrder />
            },
            {
                path: "/sales-invoice",
                element: <ObservedSalesInvoice />
            },
            {
                path: "/sales-order",
                element: <ObservedSalesOrder />
            },
            {
                path: "/company/:id",
                element: <h1>About</h1>
            },
            {
                path: "/admin/roles",
                element: <ObservedRoleList />
            },
            {
                path: "/admin/users",
                element: <ObservedUserList />
            }
        ]
    }
]);