using System;
using System.Collections.Generic;

[Serializable]
public class PredictResponse
{
    public int image_case_id;
    public int count;
    public List<DetectionBox> boxes;
    public List<BoneGroup> bone_groups;
}

[Serializable]
public class DetectionBox
{
    public float conf;
    public int cls_id;
    public string cls_name;
    public BoneInfo bone_info;
    public string sub_label;
    public string sub_label_source;
    public int? recommended_small_bone_id;
}

[Serializable]
public class BoneInfo
{
    public int bone_id;
    public string bone_en;
    public string bone_zh;
    public string bone_region;
    public string bone_desc;
}

[Serializable]
public class BoneGroup
{
    public int bone_id;
    public string bone_zh;
    public string bone_en;
    public string bone_region;
    public string bone_desc;
    public List<SmallBone> small_bones;
}

[Serializable]
public class SmallBone
{
    public int small_bone_id;
    public string small_bone_zh;
    public string small_bone_en;
    public string serial_number;
    public string place;
    public string intro_text;
    public string structure_function_text;
    public string learning_text;
    public string suggested_questions;
    public List<string> mesh_names;
    public bool has_3d_model;
}