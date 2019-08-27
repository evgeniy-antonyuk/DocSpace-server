import React from "react";
import { connect } from "react-redux";
import {
  Text,
  IconButton,
  ContextMenuButton,
  toastr
} from "asc-web-components";
import { withRouter } from "react-router";
import { isAdmin, isMe } from "../../../../../store/auth/selectors";
import { getUserStatus } from "../../../../../store/people/selectors";
import { useTranslation } from 'react-i18next';

const wrapperStyle = {
  display: "flex",
  alignItems: "center"
};

const textStyle = {
  marginLeft: "16px",
  marginRight: "16px"
};

const SectionHeaderContent = props => {
  const { profile, history, settings, isAdmin, viewer } = props;

  const onEditClick = user => {
    history.push(`${settings.homepage}/edit/${user.userName}`);
  };

  const onChangePasswordClick = () => {
    toastr.success("Context action: Change password");
  };

  const onChangePhoneClick = () => {
    toastr.success("Context action: Change phone");
  };

  const onChangeEmailClick = () => {
    toastr.success("Context action: Change e-mail");
  };

  const onDisableClick = () => {
    toastr.success("Context action: Disable");
  };

  const onEditPhoto = () => {
    toastr.success("Context action: Edit Photo");
  };

  const onEnableClick = () => {
    toastr.success("Context action: Enable");
  };
  
  const onReassignDataClick = () => {
    toastr.success("Context action: Reassign data");
  };
  
  const onDeletePersonalDataClick = user => {
    toastr.success("Context action: Delete personal data");
  };
  
  const onDeleteProfileClick = () => {
    toastr.success("Context action: Delete profile");
  };
  
  const onInviteAgainClick = () => {
    toastr.success("Context action: Invite again");
  };

  const getUserContextOptions = (user, viewer, t) => {

    let status = "";

    if(isAdmin|| (!isAdmin && isMe(user, viewer.userName))) {
      status = getUserStatus(user); 
    }

    switch (status) {
      case "normal":
      case "unknown":
        return [
          {
            key: "edit",
            label: t('EditUserDialogTitle'),
            onClick: onEditClick.bind(this, user)
          },
          {
            key: "edit-photo",
            label: t('EditPhoto'),
            onClick: onEditPhoto
          },
          {
            key: "change-email",
            label: t('EmailChangeButton'),
            onClick: onChangeEmailClick
          },
          {
            key: "change-phone",
            label: t('PhoneChange'),
            onClick: onChangePhoneClick
          },
          {
            key: "change-password",
            label: t('PasswordChangeButton'),
            onClick: onChangePasswordClick
          },
          {
            key: "disable",
            label: t('DisableUserButton'),
            onClick: onDisableClick
          }
        ];
      case "disabled":
        return [
          {
            key: "enable",
            label: t('EnableUserButton'),
            onClick: onEnableClick
          },
          {
            key: "edit-photo",
            label: t('EditPhoto'),
            onClick: onEditPhoto
          },
          {
            key: "reassign-data",
            label: t('ReassignData'),
            onClick: onReassignDataClick
          },
          {
            key: "delete-personal-data",
            label: t('RemoveData'),
            onClick: onDeletePersonalDataClick.bind(this, user)
          },
          {
            key: "delete-profile",
            label: t('DeleteSelfProfile'),
            onClick: onDeleteProfileClick
          }
        ];
      case "pending":
        return [
          {
            key: "edit",
            label: t('EditButton'),
            onClick: onEditClick.bind(this, user)
          },
          {
            key: "edit-photo",
            label: t('EditPhoto'),
            onClick: onEditPhoto
          },
          {
            key: "invite-again",
            label: t('InviteAgainLbl'),
            onClick: onInviteAgainClick
          },
          {
            key: "key5",
            label: t('DisableUserButton'),
            onClick: onDisableClick
          }
        ];
      default:
        return [];
    }
  };

  const { t } = useTranslation();
  const contextOptions = () => getUserContextOptions(profile, viewer, t);

  return (
    <div style={wrapperStyle}>
      <div style={{ width: "16px" }}>
        <IconButton
          iconName={"ArrowPathIcon"}
          color="#A3A9AE"
          size="16"
          onClick={() => history.push(settings.homepage)}
        />
      </div>
      <Text.ContentHeader truncate={true} style={textStyle}>
        {profile.displayName}
        {profile.isLDAP && ` (${t('LDAPLbl')})`}
      </Text.ContentHeader>
      {(isAdmin || isMe(viewer, profile.userName)) && (
        <ContextMenuButton
          directionX="right"
          title={t('Actions')}
          iconName="VerticalDotsIcon"
          size={16}
          color="#A3A9AE"
          getData={contextOptions}
          isDisabled={false}
        />
      )}
    </div>
  );
};

function mapStateToProps(state) {
  return {
    settings: state.auth.settings,
    viewer: state.auth.user,
    isAdmin: isAdmin(state.auth.user)
  };
}

export default connect(mapStateToProps)(withRouter(SectionHeaderContent));
