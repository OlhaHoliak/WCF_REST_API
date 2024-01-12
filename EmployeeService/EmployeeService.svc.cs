using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.ServiceModel.Web;

namespace EmployeeService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IEmployeeService
    {
        public Employee GetEmployeeById(int id)
        {
            var dt = new DataTable();
            var query = @"
WITH Descendats(ID, Name, ManagerID)
AS (SELECT e.ID,
        e.[Name],
        e.ManagerID
    FROM dbo.Employee AS e
    WHERE e.ID = @Id
    UNION ALL
    SELECT e.ID,
        e.[Name],
        e.ManagerID      
    FROM dbo.Employee AS e
    JOIN Descendats AS d ON e.ManagerID = d.ID
    WHERE e.ID != d.ID
    )
SELECT ID, Name, ManagerID
FROM Descendats
";

            var connStr = ConfigurationManager.ConnectionStrings["Test"].ConnectionString;
            using (var connection = new SqlConnection(connStr))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    try
                    {
                        connection.Open();
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dt);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new WebFaultException<string>(ex.ToString(), HttpStatusCode.InternalServerError);
                    }
                }
            }

            if(dt.Rows.Count < 1)
            {
                throw new WebFaultException(HttpStatusCode.NotFound);
            } 

            var collection = new List<Employee>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var empl = new Employee()
                {
                    ID = dt.Rows[i].Field<int>("ID"),
                    Name = dt.Rows[i].Field<string>("Name"),
                    ManagerID = dt.Rows[i].Field<int?>("ManagerID"),
                    Employees = new List<Employee>()
                };
                collection.Add(empl);
            }

            foreach (var gi in collection.Where(e => e.ID != id).GroupBy(e => e.ManagerID))
            {
                foreach (var empl in collection.Where(e => e.ID == gi.Key))
                {
                    empl.Employees = gi.ToList();
                }
            }

            var res = collection.Where(e => e.ID == id).FirstOrDefault();

            return res;
        }

        public void EnableEmployee(int id, int enable)
        {
            if (enable < 0 || enable > 1)
            {
                throw new WebFaultException<string>("'Enable' value should be 0 or 1.", HttpStatusCode.BadRequest);
            }

            int res;

            var commandStr = "UPDATE Employee SET [Enabled] = @enable WHERE ID = @id";

            var connStr = ConfigurationManager.ConnectionStrings["Test"].ConnectionString;
            using (var connection = new SqlConnection(connStr))
            {
                using (var command = new SqlCommand(commandStr, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@enable", enable);

                    try
                    {
                        connection.Open();
                        res = command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new WebFaultException<string>(ex.ToString(), HttpStatusCode.InternalServerError);
                    }
                }
            }

            if (res != 1)
            {
                throw new WebFaultException<string>($"'id'={id} is invalid, nothing was updated", HttpStatusCode.BadRequest);
            }
        }
    }
}