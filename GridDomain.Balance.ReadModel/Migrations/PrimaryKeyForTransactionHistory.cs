using System.Data.Entity.Migrations;

namespace BusinessNews.ReadModel.Migrations
{
    public partial class PrimaryKeyForTransactionHistory : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.TransactionHistories", "BusinessId");
        }

        public override void Down()
        {
            AddColumn("dbo.TransactionHistories", "BusinessId", c => c.Guid(false));
        }
    }
}