CREATE OR ALTER PROCEDURE GetUserByUserName
    @Username NVARCHAR(MAX),
	@ClientId NVARCHAR(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ClientID, UserID, Password
    FROM [dbo].clientuser
    WHERE (UserID = @Username or UserEmail = @Username) and UserStatus=1
END;
GO