using Dto;
using LiteDB;
using System.Linq;

namespace DataProvider
{
    public class CMDimensionInfoCRUD : CMDataProviderCRUDBase<CMDimensionInfoDto>
    {
        public CMDimensionInfoCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        /// <summary>
        /// Finds the first item by name
        /// </summary>
        /// <returns></returns>
        public CMDimensionInfoDto Get_ForName(string name)
        {
            var results = Find(d => d.Name.Equals(name, System.StringComparison.Ordinal));
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMDimensionInfoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                opResult.Errors.Add($"Name cannot be empty for an item in {CollectionName}");
            }

            // If we are checking an insert operation
            if (dto.Id == 0)
            {
                if (Get_ForName(dto.Name) != null)
                {
                    opResult.Errors.Add($"A dimension with the name '{dto.Name}' already exists.");
                }
            }
            // If we are checking an update operation
            else
            {
                // Find a record with the same name that is not this one
                var dupeResults = Find(f =>
                    f.Id != dto.Id
                    && f.Name.Equals(dto.Name, System.StringComparison.Ordinal));

                if (dupeResults.Any())
                {
                    opResult.Errors.Add($"A dimension with the name '{dto.Name}' already exists.");
                }
            }

            return opResult;
        }

        public CMCUDResult SaveDimensions(string name, double top, double left, double width, double height)
        {
            var dim = Get_ForName(name);
            if (dim == null)
            {
                dim = new CMDimensionInfoDto()
                {
                    Name = name
                };
            }

            dim.Top = top;
            dim.Left = left;
            dim.Width = width;
            dim.Height = height;

            return Upsert(dim);
        }

        public CMCUDResult Upsert(CMDimensionInfoDto upsertingObject)
        {
            if (upsertingObject.Id == 0)
            {
                return Insert(upsertingObject);
            }
            else
            {
                return Update(upsertingObject);
            }
        }

        public override CMCUDResult Insert(CMDimensionInfoDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMDimensionInfoDto updatingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, updatingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Update(updatingObject);
        }
    }
}
