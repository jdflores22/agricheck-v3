namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class AccreditationFormSchema
{
    /// <summary>
    /// Canonical DA accreditation form used for client applications (18 fields incl. nature_of_business).
    /// </summary>
    public const string Json = """
        [
          {"id":"2a979a88-6c3b-4006-80eb-8f9d88aa62af","name":"business_profile_information","label":"Business Profile Information","type":"section","required":false,"columnWidth":12,"displayOrder":1},
          {"id":"2d85f80f-45ab-44a8-800e-845d8012223b","name":"company_name","label":"Company Name","type":"text","required":true,"placeholder":"Company Name","tooltip":"Company Name","columnWidth":6,"displayOrder":2},
          {"id":"c9f38dfa-19e4-4d71-99e3-c0f34110fcdd","name":"business_type","label":"Business Type","type":"select","required":true,"placeholder":"Business Type","tooltip":"Choice Business Type (ex. Corporation)","columnWidth":6,"displayOrder":3,"options":[{"label":"Sole Proprietorship","value":"Sole_Proprietorship"},{"label":"Corporation","value":"Corporation"},{"label":"Partnership","value":"Partnership"},{"label":"Cooperative","value":"Cooperative"}]},
          {"id":"1360ace5-2166-4c05-af86-036c146c4fe6","name":"business_phone","label":"Business Phone #","type":"text","required":true,"columnWidth":6,"displayOrder":4},
          {"id":"cf455237-e1c7-40a3-98ba-8fd528fcddd4","name":"business_email","label":"Business Email","type":"email","required":true,"columnWidth":6,"displayOrder":5},
          {"id":"1c12319b-200c-49f5-93e3-c319d70e94c1","name":"tin_number","label":"TIN Number","type":"text","required":true,"columnWidth":6,"displayOrder":6},
          {"id":"b0583990-c245-4391-ad1c-fb3c641d7e4c","name":"years_in_operation","label":"Years in Operation","type":"text","required":true,"columnWidth":6,"displayOrder":7},
          {"id":"d65cec7f-1559-47c5-b695-8ea9b5586045","name":"number_of_employees","label":"Number of Employees","type":"text","required":true,"columnWidth":6,"displayOrder":8},
          {"id":"8c30c86d-e6d1-414f-8b3c-20bebb2dfcea","name":"nature_of_business","label":"Nature of Business","type":"text","required":true,"placeholder":"e.g. Importation of agricultural products","helpText":"Primary business activity or line of business","columnWidth":12,"displayOrder":9},
          {"id":"6224eb12-6468-433e-bce9-af1a9da87e26","name":"business_address","label":"Business Address","type":"section","required":false,"columnWidth":12,"displayOrder":10},
          {"id":"da7defdd-ec5a-42bc-9378-7d0341f254db","name":"address","label":"Address","type":"address","required":true,"placeholder":"House no., street, building, unit","helpText":"Select region, province, city/municipality, barangay, then enter street details.","columnWidth":6,"displayOrder":11},
          {"id":"b6927f6a-9516-492e-8989-12c662add79c","name":"required_documents","label":"Required Documents","type":"section","required":false,"columnWidth":12,"displayOrder":12},
          {"id":"1cc1a152-6839-4c48-b9bf-fed7e60ef273","name":"mayor_s_permit","label":"Mayor's Permit ","type":"file","required":true,"columnWidth":4,"displayOrder":13},
          {"id":"291ea398-4e49-4e3e-8da7-c704dd6fdc38","name":"bir_2303","label":"BIR 2303","type":"file","required":true,"columnWidth":4,"displayOrder":14},
          {"id":"14fedafc-a006-412e-be74-1068e19aa0f1","name":"income_tax_return","label":"Income Tax Return","type":"file","required":true,"columnWidth":4,"displayOrder":15},
          {"id":"e207071d-f5da-4580-b58b-73ea90390da2","name":"latest_gis","label":"Latest GIS","type":"file","required":true,"columnWidth":4,"displayOrder":16},
          {"id":"6f7af864-c067-44fb-89b4-c821ca0a1f75","name":"sec_registration","label":"SEC Registration","type":"file","required":true,"columnWidth":4,"displayOrder":17},
          {"id":"f89873d3-8895-4a9a-9636-3103b22b1a4f","name":"boc_cor","label":"BOC-COR","type":"file","required":true,"columnWidth":4,"displayOrder":18}
        ]
        """;
}
