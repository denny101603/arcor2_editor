using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRobot
{
    string GetName();

    string GetId();

    Task<List<string>> GetEndEffectorIds();

    RobotEE GetEE(string ee_id);

    bool HasUrdf();

    void SetJointValue(string name, float angle, bool angle_in_degrees = false);

    List<IO.Swagger.Model.Joint> GetJoints();

    void SetGrey(bool grey);

}
